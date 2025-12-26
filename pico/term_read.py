# Graciously borrowed from https://github.com/orgs/micropython/discussions/11448

import sys, select


class TermRead:
    def __init__(self):
        self.poll = select.poll()
        self.poll.register(sys.stdin, select.POLLIN)

    # Gets a byte from stdin. Returns None if there is no new data available.
    def read(self):
        if len(self.poll.poll(0)) > 0:
            return sys.stdin.buffer.read(1)
        return None
