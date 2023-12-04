#!/bin/bash

cpplint --quiet --counting=detailed --includeorder=standardcfirst --linelength=120 --recursive --exclude=android_include --exclude=include/Eigen .

pylint --rcfile=.pylintrc **/*.py  # Replace the double asterisk with the main folder of the repository

