namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
    public sealed partial class Cia
    {
        private void CountTod()
        {
            if (_todCounter > 0)
            {
                _todCounter -= _todDen;
                return;
            }

            _todCounter += _todNum * ((_cra & 0x80) != 0 ? 6 : 5);
            _tod10Ths++;
            if (_tod10Ths > 9)
            {
                _tod10Ths = 0;
                _todlo = (_todSec & 0x0F) + 1;
                _todhi = (_todSec >> 4);
                if (_todlo > 9)
                {
                    _todlo = 0;
                    _todhi++;
                }
                if (_todhi > 5)
                {
                    _todSec = 0;
                    _todlo = (_todMin & 0x0F) + 1;
                    _todhi = (_todMin >> 4);
                    if (_todlo > 9)
                    {
                        _todlo = 0;
                        _todhi++;
                    }
                    if (_todhi > 5)
                    {
                        _todMin = 0;
                        _todlo = (_todHr & 0x0F) + 1;
                        _todhi = (_todHr >> 4);
                        _todHr &= 0x80;
                        if (_todlo > 9)
                        {
                            _todlo = 0;
                            _todhi++;
                        }
                        _todHr |= (_todhi << 4) | _todlo;
                        if ((_todHr & 0x1F) > 0x11)
                        {
                            _todHr &= 0x80 ^ 0x80;
                        }
                    }
                    else
                    {
                        _todMin = (_todhi << 4) | _todlo;
                    }
                }
                else
                {
                    _todSec = (_todhi << 4) | _todlo;
                }
            }

            if (_tod10Ths == _alm10Ths && _todSec == _almSec && _todMin == _almMin && _todHr == _almHr)
            {
                TriggerInterrupt(4);
            }
        }
    }
}
