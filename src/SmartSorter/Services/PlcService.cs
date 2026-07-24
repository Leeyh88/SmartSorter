using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using EasyModbus;

namespace SmartSorter.Services
{
    public class PlcService
    {
        private ModbusClient? _modbusClient;
        private CancellationTokenSource? _cts;
        private bool _isConnected = false;
        private int _consecutiveErrorCount = 0;

        // 🔒 시리얼 포트 동시 접근(읽기/쓰기 충돌)을 막기 위한 동기화 객체
        private readonly object _syncLock = new object();

        public bool IsConnected => _isConnected;

        public event Action<bool[]?, int[]?>? OnDataRead;
        public event Action<bool, string>? OnStatusChanged;

        /// <summary>
        /// XBC-DR32H Modbus RTU 시리얼 연결
        /// </summary>
        public async Task<bool> ConnectSerialAsync(string portName, int baudRate = 9600, byte StationId = 1)
        {
            Disconnect();
            await Task.Delay(200);

            return await Task.Run(() =>
            {
                try
                {
                    lock (_syncLock)
                    {
                        _modbusClient = new ModbusClient(portName)
                        {
                            Baudrate = baudRate,
                            UnitIdentifier = StationId,
                            ConnectionTimeout = 2000, // 타임아웃 2초
                            Parity = System.IO.Ports.Parity.None,
                            StopBits = System.IO.Ports.StopBits.One
                        };

                        _modbusClient.Connect();
                    }

                    _isConnected = true;
                    _consecutiveErrorCount = 0;

                    OnStatusChanged?.Invoke(true, $"PLC 시리얼 연결 성공 ({portName}, {baudRate}bps)");

                    StartPolling();
                    return true;
                }
                catch (Exception ex)
                {
                    _isConnected = false;
                    _modbusClient = null;
                    OnStatusChanged?.Invoke(false, $"PLC 시리얼 연결 실패 ({portName}): {ex.Message}");
                    return false;
                }
            });
        }

        public void Disconnect()
        {
            _isConnected = false;

            if (_cts != null)
            {
                try { _cts.Cancel(); } catch { }
            }

            Thread.Sleep(200);

            lock (_syncLock)
            {
                if (_modbusClient != null)
                {
                    try
                    {
                        if (_modbusClient.Connected)
                        {
                            _modbusClient.Disconnect();
                        }
                    }
                    catch { }
                    _modbusClient = null;
                }
            }

            if (_cts != null)
            {
                try { _cts.Dispose(); } catch { }
                _cts = null;
            }

            OnStatusChanged?.Invoke(false, "PLC 시리얼 연결 해제됨");
        }

        /// <summary>
        /// 주기적 M접점 및 D레지스터 읽기 (Lock 동기화 적용)
        /// </summary>
        private void StartPolling()
        {
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested && _isConnected)
                {
                    try
                    {
                        bool[]? coils = null;
                        int[]? registers = null;

                        // 🛑 Lock으로 감싸서 쓰기 동작과 절대 겹치지 않게 보호
                        lock (_syncLock)
                        {
                            if (_modbusClient != null && _modbusClient.Connected)
                            {
                                coils = _modbusClient.ReadCoils(0, 10);
                                registers = _modbusClient.ReadHoldingRegisters(0, 10);
                            }
                        }

                        if (coils != null && registers != null)
                        {
                            _consecutiveErrorCount = 0;
                            OnDataRead?.Invoke(coils, registers);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _consecutiveErrorCount++;

                        // 연속 3회 이상 실패 시에만 끊김 처리 (순간 노이즈 무시)
                        if (_consecutiveErrorCount >= 3)
                        {
                            if (_isConnected)
                            {
                                _isConnected = false;
                                OnStatusChanged?.Invoke(false, $"PLC 통신 연속 에러 발생: {ex.Message}");
                            }
                            break;
                        }
                    }

                    // 폴링 타임 300ms 부여 (RS-485 릴랙스 타임)
                    try
                    {
                        await Task.Delay(300, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }, token);
        }

        /// <summary>
        /// M접점 비트 출력 (Lock 적용)
        /// </summary>
        public async Task WriteCoilAsync(int coilAddress, bool value)
        {
            if (_modbusClient == null || !_isConnected) return;

            await Task.Run(() =>
            {
                try
                {
                    lock (_syncLock)
                    {
                        if (_modbusClient != null && _modbusClient.Connected)
                        {
                            _modbusClient.WriteSingleCoil(coilAddress, value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnStatusChanged?.Invoke(false, $"Coil 쓰기 에러 (Addr: {coilAddress}): {ex.Message}");
                }
            });
        }

        /// <summary>
        /// D레지스터 워드 데이터 쓰기 (Lock 적용)
        /// </summary>
        public async Task WriteRegisterAsync(int registerAddress, int value)
        {
            if (_modbusClient == null || !_isConnected) return;

            await Task.Run(() =>
            {
                try
                {
                    lock (_syncLock)
                    {
                        if (_modbusClient != null && _modbusClient.Connected)
                        {
                            _modbusClient.WriteSingleRegister(registerAddress, value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnStatusChanged?.Invoke(false, $"Register 쓰기 에러 (Addr: {registerAddress}): {ex.Message}");
                }
            });
        }
    }
}