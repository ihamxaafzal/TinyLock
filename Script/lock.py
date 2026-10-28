"""Compatibility entry point for TinyLock's native full-screen Windows build."""
from pathlib import Path
import subprocess

if __name__ == "__main__":
    executable = Path(__file__).resolve().parent.parent / "lock.exe"
    # No arguments are forwarded: interactive launches are always full-screen.
    subprocess.Popen([str(executable)], cwd=str(executable.parent))
