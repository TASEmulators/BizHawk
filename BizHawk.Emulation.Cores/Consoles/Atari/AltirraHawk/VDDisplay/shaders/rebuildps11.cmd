@echo off
fxc /nologo /Tvs_1_1 /Fovs11_boxlinear.vsh /EVertexShaderBoxlinear1_1 displayd3d9.fx
psa /nologo /Tps_1_1 /Fops11_boxlinear.psh ps11_boxlinear.ps
fxc /nologo /Tvs_1_1 /Fovs11_pal8_to_rgb.vsh /EVS_Pal8_to_RGB_1_1 displayd3d9.fx
psa /nologo /Tps_1_1 /Fops11_pal8_to_rgb.psh ps11_pal8_to_rgb.ps
