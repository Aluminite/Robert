import time

import machine
from micropython import const
from machine import Pin

COMMAND_FORMAT = const(0b0001010101010)
BITS_MASK = const(0b1111110101010)

command_names = ("Flash LED", "Reset", "Down one", "Invalid", "Left", "Up two", "Close arms", "Invalid", "Right",
                 "LED on", "Open arms", "Invalid", "Up one", "Down two", "Invalid", "Invalid")

sensor = Pin(0, Pin.IN)


def check_command(bits):
    # Check that the command follows the structure
    if (bits & BITS_MASK) == COMMAND_FORMAT:
        # Get the meaningful bits from the command
        w = (bits & 0b0000001000000) >> 3
        x = (bits & 0b0000000010000) >> 2
        y = (bits & 0b0000000000100) >> 1
        z = (bits & 0b0000000000001)
        command = w | x | y | z
        return command
    elif bits == 0b1010101010101 or bits == 0b0101010101010:
        # Blinking the LED doesn't have a specific ID, so we're giving it zero (otherwise unused) for consistency
        return 0b0000
    else:
        return None


# 1 NES frame is 16339 us
_rob_bits_ = 0b1111111111111
_pending_commands_ = []
_last_write_ = time.ticks_us()


def sensor_detect(_):
    global _rob_bits_
    global _last_write_
    _rob_bits_ = ((_rob_bits_ << 1) & 0b1111111111111) | 0b1
    _last_write_ = time.ticks_us()
    #print(_last_write_)
    return None


def poll():
    global _rob_bits_
    global _last_write_
    irq_state = machine.disable_irq()

    command = check_command(_rob_bits_)
    if command is not None:
        _rob_bits_ = 0b1111111111111
        _last_write_ = time.ticks_add(_last_write_, 16339)
        machine.enable_irq(irq_state)
        return command

    if time.ticks_diff(time.ticks_us(), _last_write_) > 18339:
        _rob_bits_ = (_rob_bits_ << 1) & 0b1111111111111
        _last_write_ = time.ticks_add(_last_write_, 16339)
        command = check_command(_rob_bits_)
        if command is not None:
            _rob_bits_ = 0b1111111111111
            machine.enable_irq(irq_state)
            return command

    machine.enable_irq(irq_state)
    return None


def init():
    sensor.irq(trigger=Pin.IRQ_RISING, handler=sensor_detect)


def stop():
    sensor.irq(trigger=0, handler=sensor_detect)
