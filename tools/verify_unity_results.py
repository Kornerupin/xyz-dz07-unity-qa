"""A green job must contain both selected tests, not an empty test run."""
import argparse
from pathlib import Path
import xml.etree.ElementTree as ET

EXPECTED = {
    "EchoesOfAscent.Tests.PlayMode.CraftTest.Test_CraftSword_ConsumesResources",
    "EchoesOfAscent.Tests.PlayMode.CraftTest.Test_CraftSword_WithoutResources_DoesNotConsumeInventory",
}


def verify(artifacts):
    found = {}
    for path in Path(artifacts).rglob("*.xml"):
        if path.is_symlink() or path.stat().st_size > 10_000_000:
            raise ValueError("Unsafe XML results")
        raw = path.read_bytes()
        if b"<!DOCTYPE" in raw.upper() or b"<!ENTITY" in raw.upper():
            raise ValueError("XML entities are not accepted")
        try:
            root = ET.fromstring(raw)
        except ET.ParseError:
            continue
        if root.tag not in ("test-run", "test-suite"):
            continue
        for case in root.iter("test-case"):
            if case.get("result", "").lower() == "failed":
                raise ValueError("A failed test exists in the Unity results")
            name = case.get("fullname", "")
            if name in EXPECTED:
                if name in found:
                    raise ValueError("Duplicate test result")
                found[name] = case.get("result")
    if set(found) != EXPECTED or any(result != "Passed" for result in found.values()):
        raise ValueError("Both selected crafting tests must exist and pass; zero/skipped tests do not count")
    return found


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--artifacts", default="artifacts/unity")
    args = parser.parse_args()
    for name, result in verify(args.artifacts).items():
        print(f"{result}: {name}")
