#!/usr/bin/env python3
"""
Create a minimal valid NES ROM for testing the 8Bitten ROM loader.
This creates a simple NROM ROM with valid iNES header.
"""

def create_test_rom(filename="test.nes"):
    """Create a minimal valid NES ROM file."""
    
    # iNES header (16 bytes)
    header = bytearray(16)
    header[0:4] = b'NES\x1a'  # Magic number
    header[4] = 1             # 1 x 16KB PRG ROM bank
    header[5] = 1             # 1 x 8KB CHR ROM bank  
    header[6] = 0x00          # Mapper 0 (NROM), horizontal mirroring
    header[7] = 0x00          # No special features
    header[8:16] = b'\x00' * 8  # Unused bytes
    
    # PRG ROM (16KB = 16384 bytes)
    prg_rom = bytearray(16384)
    
    # Simple program: infinite loop
    # Reset vector points to 0x8000
    prg_rom[0x7FFC] = 0x00  # Reset vector low byte
    prg_rom[0x7FFD] = 0x80  # Reset vector high byte (0x8000)
    
    # Simple program at 0x8000: JMP $8000 (infinite loop)
    prg_rom[0x0000] = 0x4C  # JMP absolute
    prg_rom[0x0001] = 0x00  # Low byte of address
    prg_rom[0x0002] = 0x80  # High byte of address
    
    # CHR ROM (8KB = 8192 bytes) - can be empty for this test
    chr_rom = bytearray(8192)
    
    # Write the ROM file
    with open(filename, 'wb') as f:
        f.write(header)
        f.write(prg_rom)
        f.write(chr_rom)
    
    print(f"Created test ROM: {filename}")
    print(f"Size: {len(header) + len(prg_rom) + len(chr_rom)} bytes")
    print("ROM details:")
    print("  - Mapper: 0 (NROM)")
    print("  - PRG ROM: 16KB")
    print("  - CHR ROM: 8KB")
    print("  - Program: Infinite loop at 0x8000")

if __name__ == "__main__":
    create_test_rom()
