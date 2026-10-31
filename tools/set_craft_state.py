"""Change one expected value, never production logic. Use green/broken/green."""
import argparse
import hashlib
from pathlib import Path
import re

TEST = Path(__file__).resolve().parents[1] / "Assets/_Project/Tests/PlayMode/CraftTest.cs"
PATTERN = r"(const int expectedRemainingWood = )(3|4)(; // COURSE_MUTATION_POINT)"


def set_state(path, state):
    text = path.read_bytes().decode("utf-8-sig")
    if len(list(re.finditer(PATTERN, text))) != 1:
        raise ValueError("Expected one known mutation point; refusing to edit")
    value = {"green": "3", "broken": "4"}[state]
    result, count = re.subn(PATTERN, lambda m: m[1] + value + m[3], text)
    if count != 1:
        raise ValueError("Unexpected mutation count")
    if result != text:
        path.write_bytes(result.encode("utf-8"))
    return hashlib.sha256(path.read_bytes()).hexdigest()


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("state", choices=["green", "broken"])
    args = parser.parse_args()
    print(args.state + " CraftTest SHA256=" + set_state(TEST, args.state))
