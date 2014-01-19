# -*- Mode: Python; c-basic-offset: 4; indent-tabs-mode: nil -*-
""" This script prepares the zip file GitCraft.zip with the plugin so that it
is ready for distrbution. Before running this file, make sure that
the release version of the plugin in source/GitCraft was built!
"""
__author__ = 'exm'

import os, os.path
import zipfile

if '__main__' == __name__:
    distdir = 'dist'
    zf = zipfile.ZipFile('GitCraft.zip', 'w', zipfile.ZIP_DEFLATED)
    for root, dirnames, filenames in os.walk(distdir, followlinks=True):
        for filename in filenames:
            path = os.path.join(root, filename)
            relpath = os.path.relpath(path, distdir)
            print 'Processing %s' % relpath
            zf.write(path, relpath)
    zf.close()
