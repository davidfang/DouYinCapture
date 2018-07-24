### fiddler插件开发-DouYinCapture

- 前言

  前面几篇文章介绍了app的自动化，但是光这样只是去掉人工操作，app运行时的请求并没有抓下来。下面介绍如何通过代理实现数据的抓取 — DouYinCapture。

- 介绍

  DouYinCapture是fiddler的最基础最简单的一个插件即实现了IFiddlerExtension, IAutoTamper, IAutoTamper2接口的dll类库，只需要将这个dll文件加入到fiddler/scripts/下即能加载插件并按照我们的意愿过滤请求

- 部分代码

  ```
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
              channel.BasicPublish(exchange: "DouYin", 
                  routingKey: key,
                  basicProperties: null, 
                  body: bs);
              FiddlerApplication.Log.LogString("rabbitmq: send successfully");
  
          }
          //private static FileStream fs = new FileStream("I:\\fiddlerlog\\response\\douyin.txt", FileMode.Append, FileAccess.Write);
  
          public void AutoTamperResponseAfter(Session oSession)
          {
              if (oSession.responseCode == 200)
              {
                  sendTask(Encoding.UTF8.GetBytes(response), key);
              }
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
      }
  }
  ```

  代码很简单，唯一需要注意的用了AssemblyInfo.cs的配置必须指定fiddler的最低版本，如果不指定，到时候fiddler既不报错也不能生效插件就很骚了。下面是我的fiddler的版本

  > [assembly: Fiddler.RequiredVersion("5.0.20182.28034")]

![](https://github.com/daxingshen/imgines/raw/master/FIDDLER4.png)

- 至于如何将手机代理设置到fiddler，相关教程网上一大堆

[更多](https://www.jianshu.com/p/c542afdb9dae)
