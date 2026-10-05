using System;
using System.Threading;
using System.Threading.Tasks;

using Orion.Services;

namespace Orion.ViewModels;

public class MainViewModel : ViewModelBase
{
    private float _cementVolume;
    private float _cementFlow;
    private float _pumpVolume;
    private float _pumpFlow;

    private float _stage;
    private float _stageProgress;
    private float _totalProgress;
    private float _valveProgress;

    public float CementVolume
    {
        get => _cementVolume;
        set => SetProperty(ref _cementVolume, value);
    }

    public float CementFlow
    {
        get => _cementFlow;
        set => SetProperty(ref _cementFlow, value);
    }

    public float PumpVolume
    {
        get => _pumpVolume;
        set => SetProperty(ref _pumpVolume, value);
    }

    public float PumpFlow
    {
        get => _pumpFlow;
        set => SetProperty(ref _pumpFlow, value);
    }

    public float Stage
    {
        get => _stage;
        set => SetProperty(ref _stage, value);
    }

    public float StageProgress
    {
        get => _stageProgress;
        set => SetProperty(ref _stageProgress, value);
    }

    public float TotalProgress
    {
        get => _totalProgress;
        set => SetProperty(ref _totalProgress, value);
    }

    public float ValveProgress
    {
        get => _valveProgress;
        set => SetProperty(ref _valveProgress, value);
    }

// TODO: Temporary polling loop for the Modbus test server.
// Remove when data acquisition is moved to CoreLink.
    public async Task StartAsync()
    {
        //DEBUG Start Asyng
        Console.WriteLine(">>> StartAsync CALLED");

        using var client = new TemporaryModbusClient();

        try
        {
            await client.ConnectAsync(
                "192.168.0.100",
                502);

            //DEBUG Modbus Connect
            Console.WriteLine(">>> MODBUS CONNECTED");
        }
        catch (Exception ex)
        {
            //DEBUG Exception
            Console.WriteLine($">>> CONNECT ERROR: {ex}");
            return;
        }

        while (true)
        {
            var values = await client.ReadValuesAsync();

            CementVolume = values[0];
            CementFlow = values[1];
            PumpVolume = values[2];
            PumpFlow = values[3];

            Stage = values[4];
            StageProgress = values[5];
            TotalProgress = values[6];
            ValveProgress = values[7];

            await Task.Delay(500);
        }
    }
}