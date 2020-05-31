import os
import datetime
from multiprocessing import Pool

bizhawkPath = ""
romPath = ""
moviePath = ""

def emu(arg):
    ret = os.system(bizhawkPath + " " + romPath + " --movie=" + moviePath)
    print("Ending %d with %d at %s" % (arg,ret,datetime.datetime.now().time()))
    
if __name__ == '__main__':
    print("Starting at %s" % datetime.datetime.now().time())
    p = Pool(processes=4)
    p.map(emu,range(1))
    print("Ending parent at %s" % datetime.datetime.now().time())    

