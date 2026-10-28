using System;
using System.IO;
using UnityEngine;
namespace Deepsea {
/// <summary>Only attached by the explicit -coast-demo diagnostic; never changes the mix.</summary>
public sealed class CoastMixCapture : MonoBehaviour {
 readonly object sync=new object();float[] samples;int count,channels=2,rate;
 public void Begin(int seconds){lock(sync){rate=AudioSettings.outputSampleRate;samples=new float[rate*8*(seconds+1)];count=0;}}
 void OnAudioFilterRead(float[] data,int channelCount){
  lock(sync){if(samples==null)return;channels=channelCount;int n=Math.Min(data.Length,samples.Length-count);Array.Copy(data,0,samples,count,n);count+=n;}
 }
 public void Save(string path){
  lock(sync){
   int bytes=count*2;
   using(var w=new BinaryWriter(File.Create(path))){
    w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+bytes);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
    w.Write(16);w.Write((short)1);w.Write((short)channels);w.Write(rate);w.Write(rate*channels*2);w.Write((short)(channels*2));w.Write((short)16);
    w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(bytes);
    for(int i=0;i<count;i++)w.Write((short)(Math.Max(-1,Math.Min(1,samples[i]))*32767));
   }
   samples=null;
  }
 }
}
}
