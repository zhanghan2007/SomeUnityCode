--- Author agua.
--- CreateTime: 2021年11月26日

---@class Logger
local M = {}
M.ScriptName = "Logger"

Logger = {}
setmetatable(Logger, { __index = M })

---@field log开关
---@private
M.Enabled = true

local locktable = {}

local function TablePrint(data, cs)
    if data == nil then
        print('printtable table is nil')
    end
    local cstring = ''

    if (type(data) == 'table') then
        for i = #locktable, 1, -1 do
            if (tostring(locktable[i]) == tostring(data)) then
                return tostring(data) .. '\n'
            end
        end
    end

    table.insert(locktable, data)
    cstring = cstring .. cs .. '{\n'
    local space = cs .. '  '
    if (type(data) == 'table') then
        for k, v in pairs(data) do
            if (type(v) == 'table') then
                cstring = cstring .. space .. tostring(k) .. ' = ' .. TablePrint(v, cs)
            else
                cstring = cstring .. space .. tostring(k) .. ' = ' .. tostring(v) .. '\n'
            end
        end
    else
        cstring = cstring .. space .. tostring(data) .. '\n'
    end
    cstring = cstring .. cs .. '}\n'
    return cstring
end

local function GetPrintStr(...)
    local s = ''
    local length = select("#", ...)
    if length > 0 then
        local t = { ... }
        for i = 1, length do
            local v = t[i]
            if type(v) == 'table' then
                s = s .. ' ' .. TablePrint(v, '   ')
            else
                s = s .. ' ' .. tostring(v)
            end
        end
    end
    locktable = {}
    return s
end

---@field ... 不定参数
---最后一个参数如果是gameObject对象会在unity中project面板或hierarchy面板选中
---测试用例 Logger.Log(123, "23", { "sd", 232, "sds" }, { a = "a", s = "s" }, GameObject())
M.Log = function(...)
    if not ... then return end
    if not M.Enabled then return end

    local trace = debug.traceback()
    local obj = select(-1, ...)
    local args = { ... }

    if IsEditor and type(obj) == "userdata" and not Slua.IsNull(obj) then
        local metatable = getmetatable(obj)
        if metatable and metatable.__typename == "GameObject" then
            if #args == 1 then
                UnityEngine.Debug.Log(trace, obj)
            else
                table.remove(args)
                UnityEngine.Debug.Log(GetPrintStr(table.unpack(args)) .. '\n' .. trace, obj)
            end
            return
        end
    end

    UnityEngine.Debug.Log(GetPrintStr(...) .. '\n' .. trace)
end

---@field ... 不定参数
---最后一个参数如果是gameObject对象会在unity中project面板或hierarchy面板选中
---测试用例 Logger.LogError(123, "23", { "sd", 232, "sds" }, { a = "a", s = "s" }, GameObject())
M.LogError = function(...)
    if not ... then return end
    if not M.Enabled then return end

    local trace = debug.traceback()
    local obj = select(-1, ...)
    local args = { ... }

    if IsEditor and type(obj) == "userdata" and not Slua.IsNull(obj) then
        local metatable = getmetatable(obj)
        if metatable and metatable.__typename == "GameObject" then
            if #args == 1 then
                UnityEngine.Debug.LogError(trace, obj)
            else
                table.remove(args)
                UnityEngine.Debug.LogError(GetPrintStr(table.unpack(args)) .. '\n' .. trace, obj)
            end
            return
        end
    end

    UnityEngine.Debug.LogError(GetPrintStr(...) .. '\n' .. trace)
end

