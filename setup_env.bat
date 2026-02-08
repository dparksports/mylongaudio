@echo off
REM ============================================================
REM  Long Audio Voice Scanner - Environment Setup Script
REM  Works with RTX 4090 (CUDA 12.1) and RTX 5090 (CUDA 12.8)
REM ============================================================

echo ============================================================
echo  Long Audio Voice Scanner - Setup
echo ============================================================
echo.

REM Check Python
where python >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Python not found. Install Python 3.12+ first:
    echo         winget install Python.Python.3.12
    pause
    exit /b 1
)

python --version
echo.

REM Create venv
set VENV_PATH=%USERPROFILE%\venvs\longaudio
if exist "%VENV_PATH%\Scripts\python.exe" (
    echo [OK] Virtual environment already exists at %VENV_PATH%
) else (
    echo [SETUP] Creating virtual environment at %VENV_PATH%...
    python -m venv "%VENV_PATH%"
    if %ERRORLEVEL% neq 0 (
        echo [ERROR] Failed to create virtual environment
        pause
        exit /b 1
    )
    echo [OK] Virtual environment created
)
echo.

REM Activate venv
call "%VENV_PATH%\Scripts\activate.bat"

REM Detect GPU and choose CUDA version
echo [SETUP] Detecting GPU...
nvidia-smi --query-gpu=name --format=csv,noheader 2>nul
if %ERRORLEVEL% neq 0 (
    echo [WARNING] No NVIDIA GPU detected. Installing CPU-only PyTorch.
    set TORCH_INDEX=https://download.pytorch.org/whl/cpu
    goto :install
)

REM Check for RTX 5090 / Blackwell (needs CUDA 12.8+)
nvidia-smi --query-gpu=name --format=csv,noheader | findstr /i "5090 5080 5070" >nul
if %ERRORLEVEL% equ 0 (
    echo [SETUP] Blackwell GPU detected - using CUDA 12.8
    set TORCH_INDEX=https://download.pytorch.org/whl/cu128
) else (
    echo [SETUP] Older GPU detected - using CUDA 12.1
    set TORCH_INDEX=https://download.pytorch.org/whl/cu121
)

:install
echo.
echo [SETUP] Installing PyTorch from %TORCH_INDEX%...
pip install torch torchvision --index-url %TORCH_INDEX%

echo.
echo [SETUP] Installing faster-whisper and dependencies...
pip install faster-whisper

echo.
echo [SETUP] Upgrading pip...
python -m pip install --upgrade pip

echo.
echo ============================================================
echo  Setup Complete!
echo ============================================================
echo.
echo  Venv:    %VENV_PATH%
echo  Python:  %VENV_PATH%\Scripts\python.exe
echo.
echo  To activate: %VENV_PATH%\Scripts\activate.bat
echo  To scan:     python fast_engine.py batch_scan --no-vad --dir "C:\path\to\media"
echo  To transcribe: python fast_engine.py batch_transcribe
echo.
pause
