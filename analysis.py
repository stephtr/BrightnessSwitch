#%%
import os
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.patches as patches
import struct

filename = os.getenv('LOCALAPPDATA') + '\\BrightnessSwitch.config'
file = open(filename, mode='rb')

try:
    if file.read(2) != b'BS':
        raise Exception()
    versionFlag = file.read(1)
    if versionFlag == b'v':
        if file.read(1) != b'1':
            raise Exception()
    elif versionFlag == b'+':
        version = struct.unpack('I', file.read(4))[0]
        if version == 2:
            file.read(1)  # auto
        else:
            raise Exception()
    else:
        raise Exception()

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
    file.close()

    plt.figure(figsize=(6, 2))

    plt.plot([np.exp(b)] * 2, [-1, 1], 'k')
    # the decision criteria for the app is that a prediction ends up outside +-w / 2
    plt.gca().add_patch(patches.Rectangle(
        (np.exp(b), -1), np.exp(b - 1 / w / 2) - np.exp(b), 2, facecolor='k', alpha=0.15))
    plt.gca().add_patch(patches.Rectangle(
        (np.exp(b), -1), np.exp(b + 1 / w / 2) - np.exp(b), 2, facecolor='y', alpha=0.15))

    plt.scatter(darkList, (np.random.random(nDark) - 0.5) * 0.4,
                [entry * 100 for entry in darkWeights], 'k', alpha=0.25, linewidths=0)
    plt.scatter(lightList, (np.random.random(nLight) - 0.5) * 0.4,
                [entry * 100 for entry in lightWeights], 'y', alpha=0.25, linewidths=0)

    plt.semilogx()
    plt.xlabel('Illuminance (lux)')
    plt.ylim(-1, 1)
    plt.gca().axes.get_yaxis().set_ticks([])
    plt.tight_layout()
    plt.show()
except:
    print('Can\'t read the configuration file.')
    file.close()