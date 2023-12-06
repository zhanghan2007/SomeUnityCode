
---Http请求类
---@module Http
local Http = {}

local json = require 'rapidjson'
local HttpRequest = CS.Ty.HttpRequest
local UnityUtil = CS.Ty.UnityUtil
local Sprite = CS.UnityEngine.Sprite
local WWWForm = CS.UnityEngine.WWWForm

Http.useDnsPod = true

-- url parse begin
Http.Url = {}

local function url_decode_func(c)
    return string.char(tonumber(c, 16))
end

local function url_decode(str)
    local s = str:gsub('+', ' ')
    return s:gsub('%%(..)', url_decode_func)
end

function Http.Url.parse(u)
    local path, query = u:match '([^?]*)%??(.*)'
    if path then
        path = url_decode(path)
    end
    return path, query
end

function Http.Url.parse_query(q)
    local r = {}
    for k, v in q:gmatch '(.-)=([^&]*)&?' do
        r[decode(k)] = decode(v)
    end
    return r
end
-- url parse end

---发起异步GET请求
---@param url string 请求地址
---@param complete fun(res:table) 请求回应函数，请求成功res为{text}，text为服务器返回结果，否则为{error}
---@param timeout number 请求超时，以秒为单位，默认值为5
function Http.Get(url, complete, timeout)
    HttpRequest.Get(
        url,
        function(handler, error)
            if handler then
                complete({ text = handler.text })
            else
                complete({ error = error })
            end
        end,
        timeout or 0
    )
end

local function GetHostName(url)
    local hostName = string.gsub(url, 'https://', '')
    hostName = string.gsub(hostName, 'http://', '')

    local idx = string.find(hostName, '/')
    if idx then
        return string.sub(hostName, 1, idx - 1), string.sub(hostName, idx + 1)
    end
    return hostName, ""
end

local DnsPod = {}
local function DnsPodParse(url, complete)
    -- ios审核服不要d+
    if not Http.useDnsPod then
        complete(url)
        return
    end

    if string.match(url, '%d+.%d+.%d+.%d+') then
        complete(url)
        return
    end

    local host, addr = GetHostName(url)
    local data = DnsPod[host]
    if data then
        if os.time() < data.time then
            local newUrl = data.url .. "/" .. addr
            complete(newUrl)
            return
        end
    end

    Http.Get('http://119.29.29.29/d?dn=' .. host .. "&ttl=1",
        function(res)
            if res.error then
                complete(url)
            else
                local arr = string.split(res.text, ',')
                if arr and #arr >= 1 then
                    local newUrl = 'https://' .. arr[1]
                    DnsPod[host] = {
                        url = newUrl,
                        time = os.time() + tonumber(arr[2]) * 0.75
                    }
                    Logger.Info('host: %s => d+ host: %s', host, arr[1])
                    complete(newUrl .. "/" .. addr)
                else
                    complete(url)
                end
            end
        end,
        5
    )
end

local function safe_call(fun, target)
    return function(co)
        if target then
            xpcall_safe(fun, target, co)
        else
            xpcall_safe(fun, co)
        end
    end
end

---创建同步请求协程，协程函数内使用Http.spost可以实现阻塞试请求
---@param f fun(co:thread) 协程函数
---@param target any 如果target不为nil，协程函数为fun(target, co)
function Http.Async(f, target)
    local co = coroutine.create(safe_call(f, target))
    coroutine.resume(co, co)
end

---发起同步POST请求，需要在Http.async协程函数中调用
---@param co thread
---@param url string 请求地址
---@param data table Post数据
---@param timeout number 请求超时，以秒为单位，默认值为5
---@return table 请求成功时返回服务器回应结果，网络异常时返回{error:string}
function Http.SPost(co, url, data, timeout)
    assert(type(co) == "thread", "第一个参数需要是thread类型")
    local complete = function(result)
        if coroutine.status(co) == 'suspended' then
            coroutine.resume(co, result)
        end
    end
    Http.Post(url, data, complete, timeout)
    return coroutine.yield()
end

---发起异步POST请求
---@param url string 请求地址
---@param data table Post数据
---@param complete fun(res:table) 请求回应函数，网路异常时res为{error:string}
---@param target any 不为空时，complete函数为func(target, res)
---@param timeout number 请求超时，以秒为单位，默认值为5
function Http.Post(url, data, complete, target, timeout)
    local postData = WWWForm()
    for k, v in pairs(data) do
        postData:AddField(k, v)
    end

    DnsPodParse(url, function(newUrl)
        Logger.Info('request %s', newUrl)
        HttpRequest.Post(newUrl, postData,
            function(handler, error)
                local res
                if handler then
                    local ok
                    ok, res = pcall(json.decode, handler.text)
                    if not ok then
                        Logger.Error('json.decode fail! text=%s', tostring(text))
                        res = { error = 'json.decode fail!' }
                    end
                else
                    Logger.Error('请求失败: %s %s', newUrl, error)
                    res = { error = error }
                end

                if target then
                    complete(target, res)
                else
                    complete(res)
                end
            end,
            timeout or 5)
    end)
end

-- 下载图片
local reqSpriteQueue = {}
function Http.GetSprite(url, cb)
    local tex = UnityUtil.LoadURLTexture2D(url)
    if tex then
        local sprite = Sprite.Create(tex, Rect(0, 0, tex.width, tex.height), Vector2(0.5, 0.5))
        cb(sprite)
        return
    end

    local callbackList = reqSpriteQueue[url]
    if callbackList then
        callbackList[#callbackList + 1] = cb
        return
    end

    callbackList = {}
    callbackList[#callbackList + 1] = cb
    reqSpriteQueue[url] = callbackList

    HttpRequest.GetTexture(
        url,
        false,
        function(texture, error)
            if texture then
                UnityUtil.SaveURLTexture2D(url, texture)
                local sprite = Sprite.Create(texture, Rect(0, 0, texture.width, texture.height), Vector2(0.5, 0.5))

                callbackList = reqSpriteQueue[url]
                if callbackList then
                    for i = 1, #callbackList do
                        callbackList[i](sprite)
                    end
                end
            else
                Logger.Error("下载图片失败：%s - %s", url, error)

                callbackList = reqSpriteQueue[url]
                if callbackList then
                    for i = 1, #callbackList do
                        callbackList[i](nil)
                    end
                end
            end

            reqSpriteQueue[url] = nil
        end
    )
end

return Http
