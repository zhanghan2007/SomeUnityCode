using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using System.Net;
using System.Net.Sockets;

namespace Ty
{
    public class HttpRequest : MonoBehaviour
    {
        class MyCertificateHandler : CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData)
            {
                return true;
            }
        }
        public delegate void PostHandler(DownloadHandler handler, string error);

        public delegate void TextureHandler(Texture2D texture, string error);

        public delegate void AudioClipHandler(AudioClip clip, string error);

        private static HttpRequest _ins;

        private static HttpRequest Ins
        {
            get
            {
                if (_ins != null) return _ins;
                var go = new GameObject("[HttpRequest]");
                _ins = go.AddComponent<HttpRequest>();
                DontDestroyOnLoad(go);
                return _ins;
            }
        }

        public static void Post(string url, WWWForm postData, PostHandler handler, int timeout)
        {
            /*
            BestHTTP.HTTPRequest req = new BestHTTP.HTTPRequest(new System.Uri(url), BestHTTP.HTTPMethods.Post, (request, response) =>
            {
                if (response == null)
                {
                    handler(null, null, true, "response is null");
                    return;
                }

                if (response.IsSuccess)
                {
                    handler(response.DataAsText, response.Data, false, null);
                }
                else
                {
                    handler(null, null, true, "StatusCode:" + response.StatusCode);
                }
            });

            if (headers != null)
            {
                headers.ForEach<string, string>((key, value) =>
                {
                    req.SetHeader(key, value);
                });
            }

            if (postData != null)
            {
                postData.ForEach<string, string>((key, value) =>
                {
                    req.AddField(key, value);
                });
            }

            req.Timeout = System.TimeSpan.FromSeconds(timeout);
            req.Send();
            */

            Ins.StartCoroutine(_Post(url, postData, handler, timeout));
        }

        public static void Get(string url, PostHandler handler, int timeout)
        {
            Ins.StartCoroutine(_Get(url, handler, timeout));
        }

        public static void GetTexture(string url, bool nonReadable, TextureHandler handler)
        {
            Ins.StartCoroutine(_GetTexture(url, nonReadable, handler));
        }

        public static void GetAudioClip(string url, AudioType audioType, AudioClipHandler handler)
        {
            Ins.StartCoroutine(_GetAudioClip(url, audioType, handler));
        }

        private static IEnumerator _Post(string url, WWWForm postData, PostHandler handler, int timeout)
        {
            var req = UnityWebRequest.Post(url, postData);
            req.timeout = timeout;
            req.certificateHandler = new MyCertificateHandler();

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                handler(req.downloadHandler, null);
            }
            else
            {
                handler(null, req.error);
            }
        }

        private static IEnumerator _GetTexture(string url, bool nonReadable, TextureHandler handler)
        {
            var req = UnityWebRequestTexture.GetTexture(url, nonReadable);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                handler(((DownloadHandlerTexture) req.downloadHandler).texture, null);
            }
            else
            {
                handler(null, req.error);
            }
        }

        private static IEnumerator _GetAudioClip(string url, AudioType audioType, AudioClipHandler handler)
        {
            var req = UnityWebRequestMultimedia.GetAudioClip(url, audioType);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                handler(((DownloadHandlerAudioClip) req.downloadHandler).audioClip, null);
            }
            else
            {
                handler(null, req.error);
            }
        }

        private static IEnumerator _Get(string url, PostHandler handler, int timeout)
        {
            var req = UnityWebRequest.Get(url);
            req.timeout = timeout;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                handler(req.downloadHandler, null);
            }
            else
            {
                handler(null, req.error);
            }
        }

        public static bool IsIpv6(string hostName)
        {
            try
            {
                var host = Dns.GetHostEntry(hostName);
                var list = host.AddressList;
                if (list.Any(t => t.AddressFamily == AddressFamily.InterNetworkV6))
                {
                    return true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }

            return false;
        }
    }
}