using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace PokerCommon
{ 
    /*
        authon:zhanghan
        date:2022.10.31
        dec:
         简单的http get post 不用packet
     */
    public class BkHttpRequest : MonoBehaviour
    {
        class HttpReqIterm
        {
            public string strMethod = "";//"GET","POST"
            public string url = "";
            public string strIp = "";
            public string strHostName = "";
            public Action<int,byte[]> funcResult = null;
            public Dictionary<string, string> dicPostData = null;
            public int iTimeout = 5;//超时(秒）

            public int iResult = 0;//0.成功 ，其他失败
            public byte[] data = null;
        }
        
        private static BkHttpRequest g_BkHttpRequest = null;

        public static BkHttpRequest GetInstance() {
            if(g_BkHttpRequest == null)
            {
                GameObject obj = new GameObject("BkHttpRequest");
                DontDestroyOnLoad(obj);
                g_BkHttpRequest = obj.AddComponent<BkHttpRequest>();
            }
            return g_BkHttpRequest;
        }

        private bool m_bUseDplus = true;//是否使用d+域名解析
        private Queue m_queReq = Queue.Synchronized(new Queue());
        private Queue m_queRes = Queue.Synchronized(new Queue());
        private int m_iTreadNum = 0;//线程是否正在运行
        private int m_iMaxThreadNum = 1;//最多开启多少个线程
        private int m_iTimeout = 5;

        public void SetTimeOut(int iTimeout)
        {
            m_iTimeout = iTimeout;
            if(m_iTimeout > 10)
            {
                m_iTimeout = 10;
            }
            else if(m_iTimeout < 1)
            {
                m_iTimeout = 1;
            }
        }

        public void SetUseDplus(bool bUseDplus)
        {
            m_bUseDplus = bUseDplus;
        }

        //设置最大线程数
        public void SetMaxThreadNum(int iNum)
        {
            if(iNum > 10)
            {
                m_iMaxThreadNum = 10;
            }
            else if(iNum <= 0)
            {
                m_iMaxThreadNum = 1;
            }
            else
            {
                m_iMaxThreadNum = iNum;
            }
        }

        public void Get(string url,Action<int,byte[]> funcResult)
        {
            HttpReqIterm req = new HttpReqIterm();
            req.funcResult = funcResult;
            req.url = url;
            req.strMethod = "GET";
            if (m_bUseDplus)
            {
                // HTTPDPlus.GetInstance().GetDPlusIPArr(url, (string strIp, string strUrlNew, string strHostName) =>
                // {
                //     req.url = strUrlNew;
                //     req.strIp = strIp;
                //     req.strHostName = strHostName;
                //     m_queReq.Enqueue(req);
                // });
            }
            else
            {
                m_queReq.Enqueue(req);
            }
        }

        public void Post(string url, Dictionary<string, string> dicPostData, Action<int,byte[]> funcResult)
        {
            HttpReqIterm req = new HttpReqIterm();
            req.funcResult = funcResult;
            req.url = url;
            req.strMethod = "POST";
            req.dicPostData = dicPostData;
            if (m_bUseDplus)
            {
                // HTTPDPlus.GetInstance().GetDPlusIPArr(url, (string strIp, string strUrlNew, string strHostName) =>
                // {
                //     req.url = strUrlNew;
                //     req.strIp = strIp;
                //     req.strHostName = strHostName;
                //     m_queReq.Enqueue(req);
                // });
            }
            else
            {
                m_queReq.Enqueue(req);
            }
        }

        private void Update()
        {
            if (m_queReq.Count > 0 && m_iTreadNum < m_iMaxThreadNum)
            {
                StartReqThread(m_queReq.Dequeue() as HttpReqIterm);
            }

            if (m_queRes.Count > 0)
            {
                HttpReqIterm reqIterm = m_queRes.Dequeue() as HttpReqIterm;
                if(reqIterm != null && reqIterm.funcResult != null)
                {
                    reqIterm.funcResult(reqIterm.iResult, reqIterm.data);
                }
            }
        }
        private object _lock = new object();
        private void StartReqThread(HttpReqIterm reqIterm)
        {
            lock(_lock)
            {
                m_iTreadNum++;
                Debug.Log("StartReqThread m_iTreadNum:" + m_iTreadNum);
            }
            ThreadPool.QueueUserWorkItem((object obj) => {
                DoHttpReq(reqIterm);
                lock (_lock)
                {
                    m_iTreadNum--;
                }
            });
        }

        private void DoHttpReq(HttpReqIterm reqIterm)
        {
            //Debug.Log("BkHttpRequest.DoHttpReq reqIterm.url="+ reqIterm.url);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(reqIterm.url);
            request.Timeout = m_iTimeout * 1000;//5秒超时
            request.Method = reqIterm.strMethod;
            request.Proxy = null;//这里可以设置代理，先不用
            request.KeepAlive = true;
            if (reqIterm.strHostName.Length > 1)
            {
                request.Host = reqIterm.strHostName;
            }
            HttpWebResponse response = null;
            try
            {
                if (reqIterm.strMethod.CompareTo("POST") == 0 && reqIterm.dicPostData != null)
                {
                    int iParamNum = 0;
                    string strBody = "";
                    foreach (var info in reqIterm.dicPostData)
                    {
                        if (iParamNum > 0)
                        {
                            strBody += "&";
                        }
                        strBody += (info.Key + "=" + info.Value);
                        iParamNum++;
                    }
                    byte[] bs = Encoding.UTF8.GetBytes(strBody);
                    request.ContentType = "application/x-www-form-urlencoded;charset=utf-8";
                    request.ContentLength = bs.Length;
                    Stream reqStream = request.GetRequestStream();
                    if (reqStream != null)
                    {
                        reqStream.Write(bs, 0, bs.Length);
                        reqStream.Close();
                    }
                    else
                    {
                        reqIterm.iResult = (int)HttpStatusCode.NotAcceptable;
                        string strError = "无法连接网络";
                        reqIterm.data = Encoding.UTF8.GetBytes(strError);
                    }
                }

                response = (HttpWebResponse)request.GetResponse();
                if (response == null)
                {
                    reqIterm.iResult = (int)HttpStatusCode.NotAcceptable;
                    string strError = "无法连接网络";
                    reqIterm.data = Encoding.UTF8.GetBytes(strError);
                }
                else if (response.StatusCode == HttpStatusCode.OK)
                {
                    Stream responseStream = response.GetResponseStream();
                    int iReadLen = 256;//每次读取长度
                    byte[] buff = new byte[iReadLen];
                    int iLen = 0;
                    MemoryStream ms = new MemoryStream();
                    while (true)
                    {
                        iLen = responseStream.Read(buff, 0, iReadLen);
                        if (iLen <= 0)
                        {
                            break;
                        }
                        ms.Write(buff, 0, iLen);
                    }
                    reqIterm.data = ms.ToArray();
                    ms.Close();
                    responseStream.Close();
                }
                else
                {
                    reqIterm.iResult = -1;
                    string strError = "StatusCode" + response.StatusCode;
                    reqIterm.data = Encoding.UTF8.GetBytes(strError);
                }
            }
            catch (WebException e)
            {
                reqIterm.iResult = (int)e.Status;
                reqIterm.data = Encoding.UTF8.GetBytes(e.Message);
            }
            if (response != null)
            {
                response.Close();
            }
            m_queRes.Enqueue(reqIterm);
        }
    }
}
