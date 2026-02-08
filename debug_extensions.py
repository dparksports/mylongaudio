import os
import collections
import sys

def count_extensions(directory):
    ext_counts = collections.Counter()
    total_files = 0
    
    # Extensions currently supported in fast_engine.py
    current_support = {'.mp4', '.mkv', '.avi', '.mov', '.wav', '.mp3', '.flac', '.m4a', '.webm', '.aac'}

    print(f"Scanning directory: {directory}")
    
    for root, dirs, files in os.walk(directory):
        for f in files:
            ext = os.path.splitext(f)[1].lower()
            ext_counts[ext] += 1
            total_files += 1

    print(f"\nTotal files found: {total_files}")
    print("\nExtension Counts:")
    for ext, count in ext_counts.most_common():
        status = "[SUPPORTED]" if ext in current_support else "[MISSING]"
        print(f"{ext}: {count} {status}")

if __name__ == "__main__":
    target_dir = sys.argv[1] if len(sys.argv) > 1 else "."
    count_extensions(target_dir)
