#%%
import os
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.patches as patches
import struct

filename = os.getenv('LOCALAPPDATA') + '\\BrightnessSwitch.config'
file = open(filename, mode='rb')
header = file.read(4)

if header != b'BSv1':
    print('Can\'t read the configuration file.')
    file.close()
else:
    b = struct.unpack('d', file.read(8))[0]
    w = struct.unpack('d', file.read(8))[0]
    nDark = struct.unpack('i', file.read(4))[0]
    nLight = struct.unpack('i', file.read(4))[0]
    darkList = []
    darkWeights = []
    lightList = []
    lightWeights = []
    for i in range(nDark):
        darkList.append(np.exp(struct.unpack('d', file.read(8))[0]))
        darkWeights.append(0.2 + (i + 1) / nDark)
    for i in range(nLight):
        lightList.append(np.exp(struct.unpack('d', file.read(8))[0]))
        lightWeights.append(0.2 + (i + 1) / nLight)

    plt.figure(figsize=(6, 2))

    plt.plot([np.exp(b)] * 2, [-1, 1], 'k')
    rect = patches.Rectangle((np.exp(b - 1 / w), -1), np.exp(b + 1 / w) -
                             np.exp(b - 1 / w), 2, facecolor='k', alpha=0.1)
    plt.gca().add_patch(rect)

    plt.scatter(darkList, [0 for i in range(nDark)],
                [entry * 100 for entry in darkWeights], 'k', alpha=0.25, linewidths=0)
    plt.scatter(lightList, [0 for i in range(nLight)],
                [entry * 100 for entry in lightWeights], 'y', alpha=0.25, linewidths=0)

    plt.semilogx()
    plt.xlabel('Illuminance (lux)')
    plt.ylim(-1, 1)
    plt.gca().axes.get_yaxis().set_ticks([])
    plt.show()
