/*
    Copyright (c) Microsoft Corporation All rights reserved.  
 
    MIT License: 
 
    Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
    documentation files (the  "Software"), to deal in the Software without restriction, including without limitation
    the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
    and to permit persons to whom the Software is furnished to do so, subject to the following conditions: 
 
    The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. 
 
    THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
    TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
    THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using Microsoft.Band;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Barometer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    partial class MainPage
    {
        private App viewModel;
        double avPress = 0;
        double avTemp = 0;
        int samplesReceived = 0; // the number of Barometer samples received
                                 //         int samplesSkinReceived = 0; // the number of Skintemperature samples received
        int samplesUVReceived = 0; // the number of UV samples received
        bool UV_Flag = false;
        bool Barom_Flag = false;
        IBandInfo[] pairedBands;
        IBandClient bandClient;

        async void UV_Reading(object sender, Microsoft.Band.Sensors.BandSensorReadingEventArgs<Microsoft.Band.Sensors.IBandUVReading> args)
        {
            Microsoft.Band.Sensors.UVIndexLevel uvLev = args.SensorReading.IndexLevel;
            var uvToday = args.SensorReading.ExposureToday;
            samplesUVReceived++;
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                UV_Level.Text = uvLev.ToString();
                UV_Today.Text = uvToday.ToString();
            }); 
        }

        async void baromReading(object sender, Microsoft.Band.Sensors.BandSensorReadingEventArgs<Microsoft.Band.Sensors.IBandBarometerReading> args)
        {
            double pr = args.SensorReading.AirPressure;
            double temp = args.SensorReading.Temperature;
            avPress = avPress * (samplesReceived);
            avTemp = avTemp * (samplesReceived);
            samplesReceived++;
            avPress += pr;
            avPress /= samplesReceived;
            avTemp += temp;
            avTemp /= samplesReceived;
            double rat = 1013.25 / avPress;
            const double R = 8.31432;
            const double g = 9.80665;
            const double M = 0.0289644;
            const double Lb = 0.0065;
            const double temper = 288.15;
            double alt = (Math.Pow(rat, R * Lb / (g * M)) - 1) * temper / Lb;
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Pressure.Text = avPress.ToString("F3", CultureInfo.InvariantCulture) + " hPa";
                Temperature.Text = avTemp.ToString("F1", CultureInfo.InvariantCulture) + " °C";
                string altit = alt.ToString("F1", CultureInfo.InvariantCulture);
                Altitude.Text = altit + " m";
            });
        }

        private void UV_Click(object sender, RoutedEventArgs e)
        {
            UV_Flag = !UV_Flag;
            if (UV_Flag)
                UVButton.Content = "UV Read Stop";
            else
                UVButton.Content = "UV Read Start";
            if (UV_Flag)
                start_UV(sender, e);
            else
                stop_UV(sender, e);
        }
        private async void stop_UV(object sender, RoutedEventArgs e)
        {
            if (bandClient == null)
            {
                pairedBands = await BandClientManager.Instance.GetBandsAsync();
                if (pairedBands.Length < 1)
                {
                    this.viewModel.StatusMessage = "This sample app requires a Microsoft Band paired to your device. Also make sure that you have the latest firmware installed on your Band, as provided by the latest Microsoft Health app.";
                    return;
                }
                bandClient = await BandClientManager.Instance.ConnectAsync(pairedBands[0]);
            }
            await bandClient.SensorManager.UV.StopReadingsAsync();
            bandClient.SensorManager.UV.ReadingChanged -= UV_Reading;
            this.viewModel.StatusMessage = string.Format("Done. Samples Received: Barometer {0},  UV {1}", samplesReceived, samplesUVReceived);
            samplesUVReceived = 0;
        }

        private async void start_UV(object sender, RoutedEventArgs e)
        {
            if (bandClient == null)
            {
                pairedBands = await BandClientManager.Instance.GetBandsAsync();
                if (pairedBands.Length < 1)
                {
                    this.viewModel.StatusMessage = "This sample app requires a Microsoft Band paired to your device. Also make sure that you have the latest firmware installed on your Band, as provided by the latest Microsoft Health app.";
                    return;
                }
                bandClient = await BandClientManager.Instance.ConnectAsync(pairedBands[0]);
            }
           
           if (bandClient.SensorManager.UV.IsSupported)
           {
               bandClient.SensorManager.UV.GetCurrentUserConsent();
               bandClient.SensorManager.UV.ReadingChanged += UV_Reading;
               await bandClient.SensorManager.UV.StartReadingsAsync();
           }
           else
                this.viewModel.StatusMessage = "UV sensor is not supported with your Band version.";
              
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Barom_Flag = !Barom_Flag;
            if (Barom_Flag)
                BaromButton.Content = "Barometer Read Stop";
            else
                BaromButton.Content = "Barometer Read Start";
            if (Barom_Flag)
                start_Barom(sender, e);
            else
                stop_Barom(sender, e);
        }
        private async void stop_Barom(object sender, RoutedEventArgs e)
        {
            await bandClient.SensorManager.Barometer.StopReadingsAsync();
            bandClient.SensorManager.Barometer.ReadingChanged -= baromReading;
            this.viewModel.StatusMessage = string.Format("Done. Samples Received: Barometer {0},  UV {1}", samplesReceived, samplesUVReceived);
            samplesReceived = 0;
        }

        private async void start_Barom(object sender, RoutedEventArgs e)
        {
            this.viewModel.StatusMessage = "Running ...";
            try
            {
                // Get the list of Microsoft Bands paired to the phone.
                pairedBands = await BandClientManager.Instance.GetBandsAsync();
                if (pairedBands.Length < 1)
                {
                    this.viewModel.StatusMessage = "This sample app requires a Microsoft Band paired to your device. Also make sure that you have the latest firmware installed on your Band, as provided by the latest Microsoft Health app.";
                    return;
                }

                // Connect to Microsoft Band.
                bandClient = await BandClientManager.Instance.ConnectAsync(pairedBands[0]);
                if (!bandClient.SensorManager.Barometer.IsSupported)
                {
                        this.viewModel.StatusMessage = "Barometer sensor is not supported with your Band version. Microsoft Band 2 is required.";
                        return;
                }
                // Subscribe to Barometer data.
                
                bandClient.SensorManager.Barometer.ReadingChanged += baromReading;
                await bandClient.SensorManager.Barometer.StartReadingsAsync();
            }
            catch (Exception ex)
            {
                this.viewModel.StatusMessage = ex.ToString();
            }
        }
    }
}
