using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

[assembly: AssemblyTitle("TinyLock")]
[assembly: AssemblyDescription("Full-screen desktop privacy screen")]
[assembly: AssemblyCompany("MHamxa")]
[assembly: AssemblyProduct("TinyLock")]
[assembly: AssemblyVersion("2.1.0.0")]

static class Program {
    [STAThread] static void Main(string[] args) {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        if (args.Length == 1 && args[0] == "--self-test") { SelfTest(); return; }
        // No preview/windowed argument. Every interactive launch is full-screen.
        try { Application.Run(new LockForm(new PasswordStore(AppDomain.CurrentDomain.BaseDirectory))); }
        catch (Exception) { MessageBox.Show("TinyLock couldn't read or save its password file. Check folder permissions and keep the file on this Windows account.", "TinyLock", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    static void SelfTest() {
        string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try {
            PasswordStore store = new PasswordStore(folder);
            if (!store.Verify("1122") || store.Verify("wrong")) throw new Exception("Default password failed");
            store.Save("test-علی");
            if (!new PasswordStore(folder).Verify("test-علی") || store.Verify("1122")) throw new Exception("Persistence failed");
            using (LockForm form = new LockForm(store)) {
                if (form.FormBorderStyle != FormBorderStyle.None || !form.TopMost || form.Bounds != SystemInformation.VirtualScreen) throw new Exception("Fullscreen failed");
                if (form.Attempt("wrong")) throw new Exception("Incorrect password accepted");
                if (!form.Attempt("test-علی")) throw new Exception("Correct password rejected");
            }
            Console.WriteLine("PASS: default 1122, wrong-password rejection, salted hash persistence, fullscreen virtual-desktop bounds, topmost, borderless, successful unlock.");
        } finally { Directory.Delete(folder, true); }
    }
}

sealed class PasswordStore {
    readonly string path;
    byte[] salt, expected;
    public PasswordStore(string folder) {
        path = Path.Combine(folder, "tinylock.credentials");
        if (!File.Exists(path)) Save("1122");
        else {
            byte[] data = ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);
            if (data.Length != 48) throw new InvalidDataException();
            salt = new byte[16]; expected = new byte[32];
            Buffer.BlockCopy(data, 0, salt, 0, 16); Buffer.BlockCopy(data, 16, expected, 0, 32);
        }
    }
    static byte[] Hash(string value, byte[] salt) {
        using (var derive = new Rfc2898DeriveBytes(value, salt, 210000, HashAlgorithmName.SHA256)) return derive.GetBytes(32);
    }
    public bool Verify(string value) {
        byte[] candidate = Hash(value, salt); int different = 0;
        for (int i = 0; i < candidate.Length; i++) different |= candidate[i] ^ expected[i];
        return different == 0;
    }
    public void Save(string value) {
        byte[] newSalt = new byte[16];
        using (var random = RandomNumberGenerator.Create()) random.GetBytes(newSalt);
        byte[] newHash = Hash(value, newSalt), data = new byte[48];
        Buffer.BlockCopy(newSalt, 0, data, 0, 16); Buffer.BlockCopy(newHash, 0, data, 16, 32);
        string temp = path + ".tmp";
        File.WriteAllBytes(temp, ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser));
        if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
        salt = newSalt; expected = newHash;
    }
}

sealed class LockForm : Form {
    static readonly Color Navy = Color.FromArgb(9,19,31), Card = Color.FromArgb(16,30,45), Field = Color.FromArgb(23,42,60);
    static readonly Color White = Color.FromArgb(237,245,250), Muted = Color.FromArgb(143,165,184), Teal = Color.FromArgb(104,224,196);
    readonly PasswordStore store;
    readonly Panel center = new Panel(), card = new Panel();
    readonly Label clock = new Label(), date = new Label(), status = new Label();
    readonly TextBox input = new TextBox();
    readonly Button unlock = new Button();
    readonly Timer timer = new Timer();
    bool unlocked;
    int failures;
    DateTime retryAt = DateTime.MinValue;
    [DllImport("user32.dll", SetLastError=true)] static extern bool LockWorkStation();

    static Label LabelAt(Control parent, string text, int size, Color color, int x, int y, int width, int height, bool bold) {
        Label label = new Label { Text=text, ForeColor=color, BackColor=Color.Transparent, Font=new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular), TextAlign=ContentAlignment.MiddleCenter, Bounds=new Rectangle(x,y,width,height) };
        parent.Controls.Add(label); return label;
    }
    static Button ButtonAt(Control parent, string text, int x, int y, int width, int height, bool accent) {
        Button button = new Button { Text=text, Bounds=new Rectangle(x,y,width,height), BackColor=accent ? Teal : Card, ForeColor=accent ? Navy : Muted, FlatStyle=FlatStyle.Flat, Cursor=Cursors.Hand, Font=new Font("Segoe UI", 10, accent ? FontStyle.Bold : FontStyle.Regular), UseVisualStyleBackColor=false };
        button.FlatAppearance.BorderSize=0; button.FlatAppearance.MouseOverBackColor=accent ? Color.FromArgb(139,239,216) : Field;
        parent.Controls.Add(button); return button;
    }
    public LockForm(PasswordStore store) {
        this.store=store;
        Text="TinyLock"; FormBorderStyle=FormBorderStyle.None; StartPosition=FormStartPosition.Manual;
        Bounds=SystemInformation.VirtualScreen; TopMost=true; ShowInTaskbar=true; BackColor=Navy;
        DoubleBuffered=true; AutoScaleMode=AutoScaleMode.None;
        Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        center.BackColor=Navy; center.Size=new Size(450,570); Controls.Add(center);
        clock.Font=new Font("Segoe UI Light", 48); clock.ForeColor=White; clock.TextAlign=ContentAlignment.MiddleCenter; clock.SetBounds(0,0,450,88); center.Controls.Add(clock);
        date.Font=new Font("Segoe UI",11); date.ForeColor=Muted; date.TextAlign=ContentAlignment.MiddleCenter; date.SetBounds(0,88,450,30); center.Controls.Add(date);
        card.BackColor=Card; card.SetBounds(0,137,450,390); center.Controls.Add(card);
        card.Paint += delegate(object sender, PaintEventArgs e) { using(var pen=new Pen(Color.FromArgb(41,64,82))) e.Graphics.DrawRectangle(pen,0,0,card.Width-1,card.Height-1); };
        LabelAt(card,"◈",32,Teal,0,18,450,55,true);
        LabelAt(card,"A little peace of mind.",21,White,0,83,450,40,true);
        LabelAt(card,"Your space. Right where you left it.",10,Muted,0,126,450,25,false);
        Panel row = new Panel { BackColor=Field, Bounds=new Rectangle(32,171,386,54) }; card.Controls.Add(row);
        input.Font=new Font("Segoe UI",17); input.BorderStyle=BorderStyle.None; input.BackColor=Field; input.ForeColor=White; input.UseSystemPasswordChar=true; input.SetBounds(14,12,300,32); row.Controls.Add(input);
        Button show = ButtonAt(row,"Show",316,7,64,40,false); show.BackColor=Field;
        show.Click += delegate { input.UseSystemPasswordChar=!input.UseSystemPasswordChar; show.Text=input.UseSystemPasswordChar ? "Show" : "Hide"; input.Focus(); };
        status.Text="Enter your password to continue"; status.Font=new Font("Segoe UI",9); status.ForeColor=Muted; status.TextAlign=ContentAlignment.MiddleCenter; status.SetBounds(20,231,410,28); card.Controls.Add(status);
        unlock.Text="Unlock desktop  →"; unlock.SetBounds(32,270,386,48); unlock.Font=new Font("Segoe UI",11,FontStyle.Bold); unlock.BackColor=Teal; unlock.ForeColor=Navy; unlock.FlatStyle=FlatStyle.Flat; unlock.FlatAppearance.BorderSize=0; unlock.Cursor=Cursors.Hand; card.Controls.Add(unlock);
        unlock.Click += delegate { Attempt(input.Text); }; AcceptButton=unlock;
        Button change=ButtonAt(card,"Change password",120,332,210,36,false); change.Click += delegate { ChangePassword(); };
        LabelAt(center,"Take a breath. We'll hold your place.",10,Muted,0,536,450,30,false);
        Label brand=LabelAt(this,"◈  tinylock",19,White,38,26,190,50,true); brand.TextAlign=ContentAlignment.MiddleLeft;
        Button secure=ButtonAt(this,"Windows secure lock",30,0,220,38,false); secure.BackColor=Navy;
        secure.Click += delegate { if (!LockWorkStation()) status.Text="Windows couldn't lock this session."; };
        Resize += delegate { LayoutScreen(); secure.Location=new Point(32,ClientSize.Height-58); };
        Shown += delegate { LayoutScreen(); secure.Location=new Point(32,ClientSize.Height-58); input.Focus(); };
        FormClosing += delegate(object sender, FormClosingEventArgs e) { if (!unlocked && e.CloseReason != CloseReason.WindowsShutDown && e.CloseReason != CloseReason.TaskManagerClosing) e.Cancel=true; };
        timer.Interval=500; timer.Tick += delegate { UpdateClock(); }; timer.Start(); UpdateClock(); LayoutScreen();
    }
    void LayoutScreen() {
        Rectangle screen=Screen.FromControl(this).Bounds;
        int availableHeight=screen.Height;
        bool compact=availableHeight<730;
        clock.Visible=date.Visible=!compact;
        card.Top=compact ? 0 : 137;
        center.Height=compact ? 435 : 570;
        foreach(Control c in center.Controls) if(c is Label && c != clock && c != date) c.Top=compact ? 399 : 536;
        center.Location=new Point(screen.Left-Left+(screen.Width-center.Width)/2,screen.Top-Top+(availableHeight-center.Height)/2);
    }
    void UpdateClock() {
        clock.Text=DateTime.Now.ToString("HH:mm"); date.Text=DateTime.Now.ToString("dddd, dd MMMM yyyy");
        double remaining=(retryAt-DateTime.UtcNow).TotalSeconds;
        unlock.Enabled=remaining<=0;
        if(remaining>0) status.Text="Take a moment. Try again in "+Math.Ceiling(remaining)+"s.";
        else if(status.Text.StartsWith("Take a moment")) { status.Text="Ready when you are."; input.Focus(); }
    }
    public bool Attempt(string value) {
        if(DateTime.UtcNow<retryAt) return false;
        if(store.Verify(value)) { unlocked=true; Close(); return true; }
        failures++; input.Clear(); input.UseSystemPasswordChar=true;
        status.Text="That password isn't right. Try again."; status.ForeColor=Color.LightSalmon;
        if(failures%5==0) retryAt=DateTime.UtcNow.AddSeconds(15);
        input.Focus(); return false;
    }
    void ChangePassword() {
        using(Form dialog=new Form { Text="Change password · TinyLock", ClientSize=new Size(420,430), BackColor=Card, FormBorderStyle=FormBorderStyle.FixedDialog, MaximizeBox=false, MinimizeBox=false, StartPosition=FormStartPosition.CenterParent, TopMost=true }) {
            LabelAt(dialog,"Make it yours.",21,White,25,16,370,48,true);
            TextBox[] fields=new TextBox[3]; string[] names={"Current password","New password","Confirm new password"};
            for(int i=0;i<3;i++) { LabelAt(dialog,names[i],10,Muted,28,77+i*77,364,23,false).TextAlign=ContentAlignment.MiddleLeft; fields[i]=new TextBox { UseSystemPasswordChar=true, BackColor=Field, ForeColor=White, Font=new Font("Segoe UI",15), BorderStyle=BorderStyle.FixedSingle, Bounds=new Rectangle(28,104+i*77,364,36) }; dialog.Controls.Add(fields[i]); }
            Label feedback=LabelAt(dialog,"Use at least 4 characters.",9,Muted,20,326,380,26,false);
            Button save=ButtonAt(dialog,"Save password",28,366,364,42,true); dialog.AcceptButton=save;
            save.Click += delegate {
                if(!store.Verify(fields[0].Text)) feedback.Text="Current password is incorrect.";
                else if(fields[1].Text.Length<4 || String.IsNullOrWhiteSpace(fields[1].Text)) feedback.Text="Use at least 4 characters, not only spaces.";
                else if(fields[1].Text!=fields[2].Text) feedback.Text="The new passwords don't match.";
                else { try { store.Save(fields[1].Text); input.Clear(); status.Text="Password updated. You're all set."; status.ForeColor=Teal; dialog.Close(); } catch(IOException) { feedback.Text="Could not save. Check folder permissions."; } }
            };
            dialog.Shown += delegate { fields[0].Focus(); }; dialog.ShowDialog(this); input.Focus();
        }
    }
    protected override void OnPaint(PaintEventArgs e) {
        using(var brush=new LinearGradientBrush(ClientRectangle,Navy,Color.FromArgb(13,45,53),25f)) e.Graphics.FillRectangle(brush,ClientRectangle);
        using(var pen=new Pen(Color.FromArgb(28,54,65))) { for(int r=240;r<=600;r+=120) e.Graphics.DrawEllipse(pen,ClientSize.Width-120-r,ClientSize.Height/2-r,r*2,r*2); }
        base.OnPaint(e);
    }
    protected override void Dispose(bool disposing) { if(disposing) timer.Dispose(); base.Dispose(disposing); }
}
