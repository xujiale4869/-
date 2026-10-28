/* Portable, offline Windows launcher. Uses the installed Edge application window. */
#ifndef UNICODE
#define UNICODE
#endif
#ifndef _UNICODE
#define _UNICODE
#endif
#include <windows.h>
#include <shellapi.h>
#include <wchar.h>
#include <stdio.h>

static int fail(const wchar_t *message) {
    MessageBoxW(NULL,message,L"灯种守夜 · Lantern Ward",MB_OK|MB_ICONERROR);
    return 1;
}
static BOOL exists(const wchar_t *path) {
    DWORD attr=GetFileAttributesW(path); return attr!=INVALID_FILE_ATTRIBUTES && !(attr&FILE_ATTRIBUTE_DIRECTORY);
}
int WINAPI wWinMain(HINSTANCE instance,HINSTANCE prev,LPWSTR cmd,int show) {
    (void)prev;(void)show;
    wchar_t base[32768],folder[32768],file[32768],edge[32768],args[32768],url[32768];
    /* Diagnostics extract the exact embedded resource without starting a browser. */
    BOOL extract=(wcscmp(cmd,L"--extract-only")==0);
    DWORD n=GetEnvironmentVariableW(L"LOCALAPPDATA",base,32768);
    if(!n||n>32000) return fail(L"无法找到用户数据目录。请确认当前 Windows 账户可正常使用。");
    if(swprintf(folder,32768,L"%ls\\LanternWard",base)<0) return fail(L"数据目录路径过长。");
    if(!CreateDirectoryW(folder,NULL)&&GetLastError()!=ERROR_ALREADY_EXISTS) return fail(L"无法创建游戏数据目录。请检查目录写入权限。");
    if(swprintf(file,32768,L"%ls\\LanternWard-v01.html",folder)<0) return fail(L"游戏路径过长。");
    HRSRC res=FindResourceW(instance,MAKEINTRESOURCEW(101),RT_RCDATA);
    if(!res) return fail(L"游戏资源缺失。请重新下载完整程序。");
    HGLOBAL loaded=LoadResource(instance,res); void *data=loaded?LockResource(loaded):NULL;
    DWORD size=SizeofResource(instance,res),written=0;
    if(!data||!size) return fail(L"无法读取游戏资源。");
    HANDLE out=CreateFileW(file,GENERIC_WRITE,FILE_SHARE_READ,NULL,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,NULL);
    if(out==INVALID_HANDLE_VALUE) return fail(L"无法写入游戏文件。请检查用户目录空间和权限。");
    BOOL ok=WriteFile(out,data,size,&written,NULL); CloseHandle(out);
    if(!ok||written!=size) return fail(L"游戏文件写入不完整。请检查磁盘空间。");
    if(extract) return 0;
    const wchar_t *envs[]={L"PROGRAMFILES(X86)",L"PROGRAMFILES",L"LOCALAPPDATA"};
    BOOL found=FALSE;
    for(int i=0;i<3;i++){
        n=GetEnvironmentVariableW(envs[i],base,32768);
        if(n&&n<32000){swprintf(edge,32768,L"%ls\\Microsoft\\Edge\\Application\\msedge.exe",base);if(exists(edge)){found=TRUE;break;}}
    }
    /* Convert file path to a URL. Percent-encode UTF-8 so spaces/#/Chinese all work. */
    char utf8[65536];int bytes=WideCharToMultiByte(CP_UTF8,0,file,-1,utf8,sizeof(utf8),NULL,NULL);
    if(bytes<=0) return fail(L"游戏路径编码失败。");
    wcscpy(url,L"file:///");size_t k=wcslen(url);
    for(int i=0;i<bytes-1;i++){
        unsigned char c=(unsigned char)utf8[i];
        if(k+4>=32768) return fail(L"游戏地址过长。");
        if(c=='\\')url[k++]=L'/';
        else if((c>='a'&&c<='z')||(c>='A'&&c<='Z')||(c>='0'&&c<='9')||c=='/'||c==':'||c=='-'||c=='_'||c=='.')url[k++]=(wchar_t)c;
        else{static const wchar_t h[]=L"0123456789ABCDEF";url[k++]=L'%';url[k++]=h[c>>4];url[k++]=h[c&15];}
    }url[k]=0;
    if(found){
        /* Dedicated profile ensures persistent game saves and a clean app window. */
        int len=swprintf(args,32768,L"--app=\"%ls\" --user-data-dir=\"%ls\\BrowserData\" --no-first-run --no-default-browser-check --window-size=1280,840",url,folder);
        if(len<0) return fail(L"启动参数过长。");
        HINSTANCE result=ShellExecuteW(NULL,L"open",edge,args,NULL,SW_SHOWNORMAL);
        if((INT_PTR)result>32)return 0;
    }
    HINSTANCE result=ShellExecuteW(NULL,L"open",file,NULL,NULL,SW_SHOWNORMAL);
    if((INT_PTR)result<=32)return fail(L"未找到可用浏览器。请安装或修复 Microsoft Edge 后重试。\n也可以使用 Chrome / Edge 打开同包内的 LanternWard.html。");
    return 0;
}
