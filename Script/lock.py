import tkinter as tk
from tkinter import messagebox
from PIL import Image, ImageTk
import os, threading, pygame, sys, webbrowser
from cryptography.fernet import Fernet
import keyboard 


def resource_path(relative_path):
    if getattr(sys, 'frozen', False):
        return os.path.join(sys._MEIPASS, relative_path)
    return os.path.join(os.path.abspath("."), relative_path)


IMAGE_PATH = resource_path("4321.png")
LOCK_SOUND = resource_path("lock.wav")
ICON_PATH = resource_path("icon123.ico")


BASE_STORAGE = os.path.dirname(sys.executable if getattr(sys, 'frozen', False) else os.path.abspath(__file__))
PASSWORD_FILE = os.path.join(BASE_STORAGE, "password.enc")
KEY_FILE = os.path.join(BASE_STORAGE, "secret.key")


def load_key():
    if not os.path.exists(KEY_FILE):
        key = Fernet.generate_key()
        with open(KEY_FILE, "wb") as key_file:
            key_file.write(key)
    else:
        with open(KEY_FILE, "rb") as key_file:
            key = key_file.read()
    return key

fernet = Fernet(load_key())

def get_stored_password():
    if os.path.exists(PASSWORD_FILE):
        with open(PASSWORD_FILE, "rb") as f:
            return fernet.decrypt(f.read()).decode()
    else:
        default = "1122"
        with open(PASSWORD_FILE, "wb") as f:
            f.write(fernet.encrypt(default.encode()))
        return default

def set_stored_password(new_password):
    with open(PASSWORD_FILE, "wb") as f:
        f.write(fernet.encrypt(new_password.encode()))


class LockScreen:
    def __init__(self, root):
        self.root = root
        self.root.iconbitmap(ICON_PATH)
        self.root.attributes("-fullscreen", True)
        self.root.attributes("-topmost", True)
        self.root.configure(bg='black')
        self.root.protocol("WM_DELETE_WINDOW", self.disable_event)

        self.password = get_stored_password()
        self.setup_key_blocking()

        self.root.focus_force()
        self.root.after(100, self.keep_focus)

        self.play_lock_sound()
        self.set_background()
        self.create_widgets()
    
    def setup_key_blocking(self):
        self.blocked_hotkeys = [
            keyboard.add_hotkey("alt+tab", lambda: None, suppress=True),
            keyboard.add_hotkey("alt+f4", lambda: None, suppress=True),
            keyboard.add_hotkey("ctrl+alt+del", lambda: None, suppress=True),
            keyboard.add_hotkey("ctrl+shift+esc", lambda: None, suppress=True),
            keyboard.add_hotkey("ctrl+esc", lambda: None, suppress=True),
        ]
        self.key_hook = keyboard.on_press(self.handle_keypress)
    
    def handle_keypress(self, event):
        blocked_keys = [
            "left windows", "right windows",
            "alt", "alt gr",
            "tab", "esc"
        ]
        if event.name.lower() in blocked_keys:
            return False
        return True

    def disable_event(self, event=None):
        self.root.focus_force()
        self.root.attributes("-topmost", True)
        self.entry.focus_set()
        return "break"

    def keep_focus(self):
        if not hasattr(self, 'change_password_dialog') or not self.change_password_dialog:
            self.root.focus_force()
            self.root.attributes("-topmost", True)
            if hasattr(self, 'entry'):
                self.entry.focus_set()
        self.root.after(100, self.keep_focus)

    def play_sound(self, path):
        def sound_thread():
            pygame.mixer.init()
            pygame.mixer.music.load(path)
            pygame.mixer.music.play()
        threading.Thread(target=sound_thread, daemon=True).start()

    def play_lock_sound(self):
        self.play_sound(LOCK_SOUND)

    def set_background(self):
        try:
            image = Image.open(IMAGE_PATH)
            screen_width = self.root.winfo_screenwidth()
            screen_height = self.root.winfo_screenheight()
            image = image.resize((screen_width, screen_height), Image.LANCZOS)
            self.bg_image = ImageTk.PhotoImage(image)
            bg_label = tk.Label(self.root, image=self.bg_image)
            bg_label.place(x=0, y=0, relwidth=1, relheight=1)

            about_btn = tk.Button(self.root, text="About", font=("Arial", 8), fg="white", bg="black",
                                  bd=0, highlightthickness=0, activebackground="black", activeforeground="gray",
                                  command=self.show_about)
            about_btn.place(relx=1.0, rely=1.0, x=-10, y=-10, anchor="se")
        except Exception as e:
            messagebox.showerror("Error", f"Could not load image:\n{e}")
            self.root.destroy()
            sys.exit()

    def show_about(self):
        about_dialog = tk.Toplevel(self.root)
        about_dialog.title("About")
        about_dialog.geometry("300x150")
        about_dialog.configure(bg='black')
        about_dialog.transient(self.root)
        about_dialog.grab_set()
        about_dialog.attributes("-topmost", True)

        about_frame = tk.Frame(about_dialog, bg='black')
        about_frame.pack(pady=20, fill=tk.BOTH, expand=True)

        version_label = tk.Label(about_frame, text="App Version: 1.1", fg="white", bg="black", font=("Arial", 12))
        version_label.pack(pady=10)

        dev_frame = tk.Frame(about_frame, bg='black')
        dev_frame.pack(pady=5)

        dev_label = tk.Label(dev_frame, text="MHamxa", fg="#007bff", bg="black", font=("Arial", 12, "underline"), cursor="hand2")
        dev_label.pack(side=tk.LEFT)
        dev_label.bind("<Button-1>", lambda e: webbrowser.open("https://github.com/ihamxaafzal"))

        close_btn = tk.Button(about_dialog, text="Close", command=about_dialog.destroy)
        close_btn.pack(pady=10)

    def create_widgets(self):
        frame = tk.Frame(self.root, bg='black')
        frame.place(relx=0.5, rely=0.5, anchor=tk.CENTER)

        label = tk.Label(frame, text="Enter Password to Unlock", fg="white", bg="black", font=("Arial", 20, "bold"))
        label.pack(pady=20)

        self.entry = tk.Entry(frame, show="*", font=("Arial", 18), width=25, justify='center')
        self.entry.pack(pady=10)
        self.entry.focus_set()
        self.entry.bind("<Return>", lambda event: self.check_password())

        submit_btn = tk.Button(frame, text="Unlock", font=("Arial", 16), command=self.check_password)
        submit_btn.pack(pady=10)

        change_btn = tk.Button(frame, text="Change Password", font=("Arial", 12), command=self.open_change_password)
        change_btn.pack(pady=5)

    def check_password(self):
        if self.entry.get() == self.password:
            keyboard.unhook(self.key_hook)
            for hotkey in self.blocked_hotkeys:
                keyboard.remove_hotkey(hotkey)
            self.root.destroy()
        else:
            messagebox.showerror("Access Denied", "Incorrect Password")
            self.entry.delete(0, tk.END)

    def open_change_password(self):
        self.change_password_dialog = True
        popup = tk.Toplevel(self.root)
        popup.title("Change Password")
        popup.geometry("300x200")
        popup.configure(bg='black')
        popup.transient(self.root)
        popup.grab_set()
        popup.attributes("-topmost", True)
        popup.protocol("WM_DELETE_WINDOW", lambda: self.close_password_dialog(popup))

        form_frame = tk.Frame(popup, bg='black')
        form_frame.pack(pady=10, fill=tk.BOTH, expand=True)

        old_label = tk.Label(form_frame, text="Old Password:", fg="white", bg="black")
        old_label.pack(pady=(5, 0))

        old_pass_entry = tk.Entry(form_frame, show="*", font=("Arial", 12))
        old_pass_entry.pack(pady=(0, 5))

        new_label = tk.Label(form_frame, text="New Password:", fg="white", bg="black")
        new_label.pack(pady=(5, 0))

        new_pass_entry = tk.Entry(form_frame, show="*", font=("Arial", 12))
        new_pass_entry.pack(pady=(0, 5))

        old_pass_entry.bind("<Return>", lambda event: new_pass_entry.focus_set())

        def update_password():
            old = old_pass_entry.get()
            new = new_pass_entry.get()
            if old == self.password:
                if new:
                    set_stored_password(new)
                    self.password = new
                    messagebox.showinfo("Success", "Password changed successfully.")
                    self.close_password_dialog(popup)
                else:
                    messagebox.showerror("Error", "New password cannot be empty.")
            else:
                messagebox.showerror("Error", "Old password is incorrect.")

        button_frame = tk.Frame(popup, bg='black')
        button_frame.pack(pady=10)

        change_button = tk.Button(button_frame, text="Change", command=update_password)
        change_button.pack(side=tk.LEFT, padx=10)

        cancel_button = tk.Button(button_frame, text="Cancel", command=lambda: self.close_password_dialog(popup))
        cancel_button.pack(side=tk.LEFT, padx=10)

        old_pass_entry.focus_set()

        def tab_between_fields(event):
            if event.widget == old_pass_entry:
                new_pass_entry.focus_set()
                return "break"
            elif event.widget == new_pass_entry:
                old_pass_entry.focus_set()
                return "break"

        old_pass_entry.bind("<Tab>", tab_between_fields)
        new_pass_entry.bind("<Tab>", tab_between_fields)

        old_pass_entry.bind("<Button-1>", lambda e: old_pass_entry.focus_set())
        new_pass_entry.bind("<Button-1>", lambda e: new_pass_entry.focus_set())

    def close_password_dialog(self, popup):
        self.change_password_dialog = False
        popup.destroy()
        self.root.focus_force()
        self.entry.focus_set()


if __name__ == "__main__":
    root = tk.Tk()
    app = LockScreen(root)
    root.mainloop()
