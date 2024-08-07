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

        public void AddOnAccountRegistrationStateChangedDelegate(OnAccountRegistrationStateChangedDelegate myDelegate)
        {
            Core.Listener.OnAccountRegistrationStateChanged += myDelegate;
        }

        public void RemoveOnAccountRegistrationStateChangedDelegate(OnAccountRegistrationStateChangedDelegate myDelegate)
        {
            Core.Listener.OnAccountRegistrationStateChanged -= myDelegate;
        }

        public void AddOnCallStateChangedDelegate(OnCallStateChangedDelegate myDelegate)
        {
            Core.Listener.OnCallStateChanged += myDelegate;
        }

        public void RemoveOnCallStateChangedDelegate(OnCallStateChangedDelegate myDelegate)
        {
            Core.Listener.OnCallStateChanged -= myDelegate;
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

            // Initiate an outgoing call to the given destination Address.
            Core.InviteAddress(address);
        }

        public bool ToggleMic()
        {
            return Core.MicEnabled = !Core.MicEnabled;
        }

        public bool ToggleSpeaker()
        {
            return Core.CurrentCall.SpeakerMuted = !Core.CurrentCall.SpeakerMuted;
        }

        /// <summary>
        /// Ask the peer of the current call to enable/disable the video call.
        /// </summary>
        public async Task<bool> ToggleCameraAsync()
        {
            await OpenCameraPopup();

            // Retrieving the current call
            Call call = Core.CurrentCall;

            // Core.createCallParams(call) create CallParams matching the Call parameters,
            // here the current call. CallParams contains a variety of parameters like
            // audio bandwidth limit, media encryption type...< And if the video is enable
            // or not.
            CallParams param = core.CreateCallParams(call);

            // Switch the current VideoEnableValue
            bool newValue = !param.VideoEnabled;
            param.VideoEnabled = newValue;
            param.VideoDirection = MediaDirection.RecvOnly;

            // Try to update the call parameters with those new CallParams.
            // If the video switched from true to false the peer can't refuse to disable the video.
            // If the video switched from false to true and the peer doesn't have videoActivationPolicy.AutomaticallyAccept = true
            // you have to wait for them to accept the update. The Call status is "Updating" during this time.
            call.Update(param);

            return newValue;
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

        private async Task OpenCameraPopup()
        {
            MediaCapture mediaCapture = new MediaCapture();
            try
            {
                await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.Video
                });
            }
            catch (Exception e) when (e.Message.StartsWith("No capture devices are available."))
            {
                // Ignored. You can ask the remote party for video even if you don't have a camera.
            }
            mediaCapture.Dispose();
        }
    }
}