import os
import shutil
import subprocess
import sys

def verify_fix():
    test_dir = "test_media_scan"
    if os.path.exists(test_dir):
        shutil.rmtree(test_dir)
    os.makedirs(test_dir)

    # Create dummy files
    extensions = ['.mp3', '.wma', '.ogg', '.m4v', '.3gp', '.ts', '.mpg', '.txt']
    for ext in extensions:
        with open(os.path.join(test_dir, f"test{ext}"), "w") as f:
            f.write("dummy content")

    print(f"Created test files in {test_dir} with extensions: {extensions}")

    # Run scanner
    print("\nRunning fast_engine.py scan...")
    # Use the current python interpreter to run fast_engine.py
    cmd = [sys.executable, "fast_engine.py", "batch_scan", "--dir", test_dir, "--no-vad"]
    
    try:
        result = subprocess.run(cmd, capture_output=True, text=True, check=True)
        print(result.stdout)
        
        # Check if output contains the expected files
        missing = []
        for ext in extensions:
            if ext == '.txt': continue # .txt should NOT be detected
            expected_file = f"test{ext}"
            if expected_file not in result.stdout:
                missing.append(ext)
        
        if not missing:
            print("\nSUCCESS: All expected extensions were detected!")
        else:
            print(f"\nFAILURE: The following extensions were NOT detected: {missing}")

    except subprocess.CalledProcessError as e:
        print(f"Error running scan: {e}")
        print(e.stdout)
        print(e.stderr)
    finally:
        # Cleanup
        if os.path.exists(test_dir):
            shutil.rmtree(test_dir)

if __name__ == "__main__":
    verify_fix()
