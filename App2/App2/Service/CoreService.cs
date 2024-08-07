/*
 * Copyright (c) 2010-2020 Belledonne Communications SARL.
 *
 * This file is part of Linphone TutorialCS.
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using Linphone;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.UI.Core;
using static Linphone.CoreListener;

namespace _03_OutgoingCall.Service
{
    internal class CoreService
    {
        private Timer Timer;

        private static readonly CoreService instance = new CoreService();

        public static CoreService Instance
        {
            get
            {
                return instance;
            }
        }

        private Core core;

        public Core Core
        {
            get
            {
                if (core == null)
                {
                    Factory factory = Factory.Instance;

                    string assetsPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "share");
                    factory.TopResourcesDir = assetsPath;
                    factory.DataResourcesDir = assetsPath;
                    factory.SoundResourcesDir = Path.Combine(assetsPath, "sounds", "linphone");
                    factory.RingResourcesDir = Path.Combine(factory.SoundResourcesDir, "rings");
                    factory.ImageResourcesDir = Path.Combine(assetsPath, "images");
                    factory.MspluginsDir = ".";

                    core = factory.CreateCore("", "", IntPtr.Zero);

                    core.AudioPort = 7666;
                    core.VideoPort = 9666;

                    core.RootCa = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "share", "Linphone", "rootca.pem");
                    core.UserCertificatesPath = ApplicationData.Current.LocalFolder.Path;

                    // NEW!
                    VideoActivationPolicy videoActivationPolicy = factory.CreateVideoActivationPolicy();
                    videoActivationPolicy.AutomaticallyAccept = true;
                    videoActivationPolicy.AutomaticallyInitiate = false;
                    core.VideoActivationPolicy = videoActivationPolicy;

                    core.VideoCaptureEnabled = core.VideoSupported();
                    core.UsePreviewWindow(true);
                }
                return core;
            }
        }

        public void CoreStart(CoreDispatcher dispatcher)
        {
            Core.Start();

            Timer = new Timer(OnTimedEvent, dispatcher, 20, 20);
        }

        private async void OnTimedEvent(object state)
        {
            await ((CoreDispatcher)state).RunIdleAsync((args) =>
            {
                Core.Iterate();
            });
        }

        /// <summary>
        /// Make a call.
        /// </summary>
        public async void Call(string uriToCall)
        {
            // We call this method to pop the microphone permission window.
            // If the permission was already granted for this app, no pop up
            // appears.
            await OpenMicrophonePopup();

            // We create an Address object from the URI.
            // This method can create an SIP Address from a username
            // or phone number only.
            Address address = Core.InterpretUrl(uriToCall);

            var audioDevice = Core.ExtendedAudioDevices;
            foreach (var item in audioDevice)
            {
                Debug.WriteLine(item.DeviceName);
                if (item.DeviceName.Contains("스테레오 믹스"))
                {
                    Core.DefaultInputAudioDevice = item;
                }
            }

            // Initiate an outgoing call to the given destination Address.
            Core.InviteAddress(address);
        }

        public async Task OpenMicrophonePopup()
        {
            AudioGraphSettings settings = new AudioGraphSettings(Windows.Media.Render.AudioRenderCategory.Media);
            CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);
            AudioGraph audioGraph = result.Graph;

            CreateAudioDeviceInputNodeResult resultNode = await audioGraph.CreateDeviceInputNodeAsync(Windows.Media.Capture.MediaCategory.Media);
            AudioDeviceInputNode deviceInputNode = resultNode.DeviceInputNode;

            deviceInputNode.Dispose();
            audioGraph.Dispose();
        }

        public void getAudioDevices()
        {
            IEnumerable<AudioDevice> audioDevices = Core.ExtendedAudioDevices;
            foreach (var audioDevice in audioDevices)
            {
                if(audioDevice.Capabilities == AudioDeviceCapabilities.CapabilityPlay)
                {
                    Debug.WriteLine($"스피커 : {audioDevice.DeviceName}");
                }else if (audioDevice.Capabilities == AudioDeviceCapabilities.CapabilityRecord)
                {
                    Debug.WriteLine($"마이크 : {audioDevice.DeviceName}");
                }
            }
        }

    }
}