from machine import Pin
import rp2

import rob_input
from nes_controller import nes_controller
from term_read import TermRead

clock_pin = Pin(1, Pin.IN)
latch_pin = Pin(2, Pin.IN)
data_pin = Pin(3, Pin.OUT, value=1)

controller = rp2.StateMachine(0, nes_controller, freq=10000000, in_base=clock_pin, out_base=data_pin, set_base=data_pin)
controller.put(0b11111111)

stdin = TermRead()

try:
    rob_input.init()
    controller.active(1)
    controller_byte = 0b11111111
    input_skipped = False
    while True:
        command = rob_input.poll()
        if command is not None:
            print("%x" % command, end='')

        stdin_byte = stdin.read()
        if stdin_byte is not None:
            if stdin_byte == b'A':  # Press A
                controller_byte = controller_byte & 0b11111110
            elif stdin_byte == b'a':  # Release A
                controller_byte = controller_byte | 0b00000001
            elif stdin_byte == b'B':  # Press B
                controller_byte = controller_byte & 0b11111101
            elif stdin_byte == b'b':  # Release B
                controller_byte = controller_byte | 0b00000010
            # All other inputs are ignored.

            if controller.tx_fifo() < 8:
                # The PIO will block if the output FIFO is full.
                # This should only happen if the controller isn't actually connected,
                # or if inputs are being sent faster than 60 times/sec.
                # If the FIFO *is* full, the new input is not sent until there's space.
                controller.put(controller_byte)
            else:
                input_skipped = True

        if input_skipped and controller.tx_fifo() < 8:
            controller.put(controller_byte)
            input_skipped = False

finally:
    rob_input.stop()
    controller.active(0)
