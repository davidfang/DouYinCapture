using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fiddler;
using RabbitMQ.Client;

namespace DouYinCaptuer
{ 
    public class Douyin : IFiddlerExtension, IAutoTamper, IAutoTamper2
    {
        private static IModel channel;
        private void sendTask(byte[] bs, string key)
        {
            try 
            {
                channel.BasicPublish(exchange: "DouYin",
                routingKey: key,
                basicProperties: null,
                body: bs);
                FiddlerApplication.Log.LogString("rabbitmq: send successfully");
            }
            catch (Exception)
            {

            }
        }
        //private static FileStream fs = new FileStream("I:\\fiddlerlog\\response\\douyin.txt", FileMode.Append, FileAccess.Write);
        public void AutoTamperRequestAfter(Session oSession)
        {
        }
         
        public void AutoTamperRequestBefore(Session oSession)
        {
        } 

        public void AutoTamperResponseAfter(Session oSession)
        {
            if (oSession.responseCode == 200)
            {
                String path = oSession.PathAndQuery.Split('?')[0];
                String queue = "";
                String key = "";
                switch (path)
                {
                    case "/aweme/v1/feed/":
                        // 首页列表
                        queue = "douyin.author";
                        key = "douyin.author.feed";
                        break;
                    case "/aweme/v1/user/":
                        // 主播信息
                        queue = "douyin.author";
                        key = "douyin.author.info";
                        break;
                    case "/aweme/v1/aweme/post/":
                        // 主播作品
                        queue = "douyin.author";
                        key = "douyin.author.post";
                        break;
                    case "/aweme/v1/aweme/favorite/":
                        // 主播喜欢的视频
                        queue = "douyin.author";
                        key = "douyin.author.favorite";
                        break;
                    case "/aweme/v1/user/following/list/":
                        // 主播关注
                        queue = "douyin.author";
                        key = "douyin.author.following";
                        break;
                    case "/aweme/v1/user/follower/list/":
                        // 主播粉丝
                        queue = "douyin.author";
                        key = "douyin.author.follower";
                        break;
                    case "/aweme/v1/music/detail/":
                        // 音乐人信息
                        queue = "douyin.music";
                        key = "douyin.music.detail";
                        break;
                    case "/aweme/v1/music/fresh/aweme/": 
                        // 使用某音乐最新的作品
                        queue = "douyin.music";
                        key = "douyin.music.fresh";
                        break;
                    case "/aweme/v1/music/aweme/":
                        // 使用某音乐的作品中最热的
                        queue = "douyin.music";
                        key = "douyin.music.hot";
                        break;
                    case "/aweme/v1/comment/list/":
                        //评论列表
                        queue = "douyin.author";
                        key = "douyin.author.comment";
                        break;
                    default: 
                        return;
                } 
                if (queue != "")
                {
                    // # 提取uid 放到response中
                    String uid_pattern = "user_id=(?<uid>\\d{1,13})&";
                    String mid_pattern = "music_id=(?<mid>\\d{1,20})&";
                    Match uid_match = Regex.Match(oSession.PathAndQuery, uid_pattern);
                    Match mid_match = Regex.Match(oSession.PathAndQuery, mid_pattern);
                   
                    /* String pattern = "\"uid\" ?: ?\"(?<uid>\\d{11,12})\"";
                     MatchCollection matches = Regex.Matches(oSession.GetResponseBodyAsString(), pattern);

                     foreach (Match match in matches)
                     {
                         fs.Write(Encoding.UTF8.GetBytes(match.Groups["uid"]+"\r\n"), 0, match.Groups["uid"].Length+"\r\n".Length);
                     }
                     */
                    string response = oSession.GetResponseBodyAsString().Trim();
                    string post_time = ", \"post_time\":\"" + GetTimestamp().ToString() + "\"}";
                    response = response.Substring(0, response.Length - 1) + post_time;
                    if (uid_match.Success)
                    {
                        string uid = ", \"uid\":\"" + uid_match.Groups["uid"] + "\"}";
                        response = response.Substring(0, response.Length - 1) + uid;
                    }
                    if (mid_match.Success) 
                    {
                        string mid = ", \"mid\":\"" + mid_match.Groups["mid"] + "\"}";
                        response = response.Substring(0, response.Length - 1) + mid;
                    }
                    sendTask(Encoding.UTF8.GetBytes(response), key);
                }
            }
        }

        public void AutoTamperResponseBefore(Session oSession)
        {
        }

        public void OnBeforeReturningError(Session oSession)
        {
        } 

        public void OnBeforeUnload()
        {
            FiddlerApplication.Log.LogString("卸载抖音插件成功");
            // fs.Close();
            channel.Close();
            channel.Dispose();
        }

        public void OnLoad()
        {
            FiddlerApplication.Log.LogString("加载抖音插件成功");
            ConnectionFactory rbmqfactory = new ConnectionFactory()
            {
                HostName = "",
                Port = 5672,
                UserName = "", 
                Password = ""
            };
            IConnection con = rbmqfactory.CreateConnection();
            channel = con.CreateModel();
            FiddlerApplication.Log.LogString("连接RabbitMQ成功");
        }
          
        public void OnPeekAtResponseHeaders(Session oSession)
        {
        }
        public static long GetTimestamp()
        {
            TimeSpan ts = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1);//ToUniversalTime()转换为标准时区的时间,去掉的话直接就用北京时间
            return (long)ts.TotalSeconds;//获取10位
        }
    }
}
