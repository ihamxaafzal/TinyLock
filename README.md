# 🔐 TinyLock | Desktop Lock | Child Lock for Windows PC

**TinyLock** is a fullscreen, password-protected **desktop lockscreen** for Windows. Designed for parents, educators, and individual users, it helps lock the screen and prevent children or others from accessing your computer without the correct password.

---

## ✅ Features

- 🖥️ **Fullscreen Lock**: Prevents access to anything on your desktop until the correct password is entered.
- 🔐 **Encrypted Password**: Password is stored securely using `cryptography.fernet` (AES-based).
- 🔒 **Key Combination Blocking**: Blocks Alt+Tab, Alt+F4, Ctrl+Esc, Ctrl+Shift+Esc.
- 🎨 **Custom Background and Icon**.
- 🔁 **Password Change Option** (requires current password).
- 🧠 **Focus Enforcement**: Continuously re-focuses on the lockscreen.

---

## 🧑‍💻 How It Works

1. Launch `TinyLock.exe` — the app will go fullscreen immediately.
2. A password prompt appears — **default password: `1122`**.
3. If you enter the wrong password:
   - An error message is shown.
4. If you enter the correct password:
   - The app closes and access to the desktop is restored.
5. You can also click **"Change Password"** to update it securely.

---

## 🔐 Security Notes

- Password and encryption key are saved next to the `.exe` as:
  - `password.enc` (encrypted)
  - `secret.key` (used for encryption/decryption)
- Disables Task Manager by editing Windows Registry.
- You can restore Task Manager using the script below.

---

## 🖼 Included Files

| File           | Description                      |
|----------------|----------------------------------|
| `TinyLock.exe` | Main executable file             |
| `4321.png`     | Fullscreen background image      |
| `lock.wav`     | Lock sound                       |
| `icon123.ico`  | Application icon (embedded)      |
| `password.enc` | Encrypted password file          |
| `secret.key`   | AES key file for decryption      |

---

## 🧑‍🔧 System Requirements

- OS: **Windows 10 or 11**
- Python: **Not required** (precompiled `.exe`)
- Must be **run as administrator** for full protection (auto-prompts on launch)



➡️ Run this script once using Python.

---

## ⚠️ Disclaimer

This app is **meant for casual or home use**. It does not provide absolute system-level security and **should not be relied on to protect sensitive or critical data**. It is designed to discourage casual access or act as a child lock.

---

## 📥 Download

You can download the compiled `.exe` file from the [Releases]([(https://github.com/ihamxaafzal/TinyLock/blob/main/lock.exe)) section.
