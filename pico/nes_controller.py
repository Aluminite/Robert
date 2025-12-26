import rp2

try:
    from typing_extensions import TYPE_CHECKING  # type: ignore
except ImportError:
    TYPE_CHECKING = False
if TYPE_CHECKING:
    from rp2.asm_pio import *


# Controller buttons are encoded in a single byte (active low) with the following order (MSB to LSB):
# Right, Left, Down, Up, Start, Select, B, A

# Input pins: Clock, Latch
# Output/set pin: Data
@rp2.asm_pio(out_init=rp2.PIO.OUT_HIGH, set_init=rp2.PIO.OUT_HIGH, out_shiftdir=rp2.PIO.SHIFT_RIGHT,
             pull_thresh=8, autopull=False, fifo_join=rp2.PIO.JOIN_TX)
def nes_controller():
    wrap_target()
    wait(1, pin, 1)  # Wait for latch to go high
    pull(noblock)  # Get new controller byte
    mov(x, osr)  # Save byte in x for next time if no new byte is sent for next latch

    out(pins, 1)  # Output the first bit to data
    wait(0, pin, 1)  # Wait for latch to go low

    label("clock_loop")  # Loop for the next 7 buttons
    wait(0, pin, 0)  # Wait for clock to go low
    wait(1, pin, 0)  # Wait for clock to go high
    out(pins, 1)  # Output next bit to data
    jmp(not_osre, "clock_loop")

    # Wait for the final clock and then set data low
    wait(0, pin, 0)
    wait(1, pin, 0)
    set(pins, 0)
    wrap()
