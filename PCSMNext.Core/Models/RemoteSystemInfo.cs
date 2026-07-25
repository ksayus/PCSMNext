using System;
using System.Collections.Generic;
using System.Text;

namespace PCSMNext.Core.Models;

/// <summary>
/// remote system infomation
/// </summary>
public class RemoteSystemInfo
{
    // example: Ubantu 22.04
    public string OsName { get; set; } = "";
    // example: 5.15.0-91-generic
    public string KernelVersion { get; set; } = "";
    // example: 12.5%
    public string CpuUsage { get; set; } = "";
    // example: 2.1 G / 4 G
    public string MemoryUsage { get; set; } = "";
    // example: 45 G / 100 G
    public string DiskUsage { get; set; } = "";
    // remote Java version
    public string JavaVersion { get; set; } = "";
    // system run time
    public string Uptime { get; set; } = "";
}