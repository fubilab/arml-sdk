#!/bin/bash

cpplint --quiet --counting=detailed --includeorder=standardcfirst --linelength=120 --recursive .  # Replace the dot with the main folder of the repository
pylint --rcfile=.pylintrc **/*.py  # Replace the double asterisk with the main folder of the repository

