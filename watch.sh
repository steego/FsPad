#!/bin/bash

sigint_handler() {
    kill $PID
    exit
}

trap sigint_handler SIGINT

while true; do
    $@ &
    PID=$!
    fswatch -1 `pwd`
    kill $PID
done