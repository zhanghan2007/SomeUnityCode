require "GameBoss/Test/Common/TestHeader"

local Http = require "GameBoss/Core/Net/BossHttp"
local Socket = require "GameBoss/Core/Net/BossSocket"
local TestCharacter = require "GameBoss/Test/Character/TestCharacter"
local UnityUtil = CS.XMJJ.UnityUtil

---@class TestClient
local TestClient = class("TestClient")

---登录地址
-- local SERVER_URL = "http://192.168.13.196:30000" -- 内网测试服
local SERVER_URL = "http://192.168.13.196:32000" -- 内网线上服 - 精简服务器
local LOGIN_URL = SERVER_URL .. "/p/v1/login/username/xmjj"

---初始化
function TestClient:ctor()
    self:ConnectGame()
end

local account, session, gameID, zoneID

function TestClient.GetAgentID()
    return "1"
end

function TestClient.GetImei()
    return CS.UnityEngine.SystemInfo.deviceModel
end

function TestClient.GetDeviceType()
    return 1
end

function TestClient.GetVersion()
    return "1.0.201"
end

function TestClient.GetDeviceId()
    return UnityUtil.GetDeviceId()
end

function TestClient.GetMac()
    return ""-- UnityUtil.GetMacAddress()
end

local function MakeVector3(serverPos)
    return Vector3(serverPos.x, serverPos.y, serverPos.z)
end

local function MakeProtoPos(vector3)
    return {x = vector3.x, y = vector3.y, z = vector3.z}
end

local function Login(co)
    local res = Http.spost(co, LOGIN_URL, {
        userId = 'whu66',
        userToken = '1111',
        device = TestClient.GetDeviceId(),
        mac = TestClient.GetMac(),
        cVersion = TestClient.GetVersion(),
        agentID = TestClient.GetAgentID(),
        imei = TestClient.GetImei()
    })
    Logger.Info("登录结果：%s", res)

    return res
end

function TestClient:ConnectGame()
    Http.async(function(co)
        local res
        local loginCount = 0
        repeat
            loginCount = loginCount + 1
            res = Login(co)

            if loginCount >= 3 then
                Logger.Error("登录失败：%s",tostring(res.r))
                return
            end
        until res.r == 0

        account = res.accountID
        session = res.session

        res = Http.spost(co, SERVER_URL .. "/p/v1/last_server", {
            u = account,
            s = session
        })
        Logger.Info("服务器列表：%s", res)

        if res.r ~= 0 then
            Logger.Error("拉取服务器列表失败：%s", tostring(res.r))
            return
        end
        gameID = res.lastGameID -- 游戏ID
        zoneID = tonumber(res.lastZoneID)

        res = Http.spost(co, SERVER_URL.."/p/v1/server_list", {
            u = account,
            s = session,
            gameID = gameID
        })
        Logger.Info("仙魔拉取服务器列表:%s", table.tostring(res))

        zoneID = zoneID > 0 and zoneID or res.zones[1].zoneID

        local res2 = Http.spost(co, SERVER_URL .. "/p/v1/select_zone", {
            u = account,
            s = session,
            gameID = gameID,
            zoneID = zoneID
        })

        Logger.Info("登录游戏服(zone=%d)：%s", zoneID, res2)

        if res2.r ~= 0 then
            Logger.Error("登录游戏服(zone=%d)失败：%s", zoneID, tostring(res2.r))
            return
        end

        local hosts = string.split(res2.serverHost, ":")
        local ip, port = hosts[1], hosts[2]
        local socket = Socket.sconnect(co, ip, port)
        if not socket then
            Logger.Error("Connect Failed: ip:%s, port:%s", ip, port)
            return
        end
        Logger.Info("Connect Successed: ip:%s, port:%s", ip, port)

        socket:msg(self)

        local protos = require "GameBoss/Common/Protocol/BossAgentProto"
        socket:setSproto(protos)

        -- 加解密
        local token = session
        local key = token:sub(17, 32)
        local crypto = CS.XMJJ.AESCrypto(key, key)
        crypto.EnableEncrypt = false -- 第一个发送包不加密
        socket:setCrypto(crypto)

        local msgType = "C2SShakeHands"
        local res = socket:scall(co, msgType, {
            accountID = account,
            token = token,
            device = TestClient.GetDeviceId(),
            mac = TestClient.GetMac(),
            cVersion = TestClient.GetVersion(),
            agentID = TestClient.GetAgentID(),
        }, 30)
        Logger.Info("%s: %s", msgType, res)

        if res.result ~= 0 then
            Logger.Error("%s Failed:%d", msgType, res.result)
            return
        end

        -- 加解密
        socket:getCrypto().EnableEncrypt = true

        Logger.Info("游戏服握手成功")

        res = socket:scall(co, "C2SGetRole", {device = UnityUtil.GetDeviceId()}, 30)
        if res.result ~= 0 then
            Logger.Error("获取角色信息失败：" .. res.result)
            return
        end

        res = socket:scall(co, "C2SEnterWorld", {roleIndex=0}, 30)
        if res.result ~= 0 then
            Logger.Error("进入游戏地图失败：" .. res.result)
            return
        end

        local mapID = res.mapID
        mapID = mapID % 100000, mapID // 100000

        -- 地图加载完成-通知服务器
        Logger.Debug("通知服务器地图加载完成")
        socket:send("C2SMapLoadComplete", {clientTime = Time.realtimeSinceStartup})

        self.socket = socket
        self.characters = {}
    end)
end

function TestClient:Update(deltaTime)
    self:UpdateRoleControl(deltaTime)
end

function TestClient:UpdateRoleControl(deltaTime)
    if not self.role then
        return
    end

    local wDir = Vector3.zero
    if Input.GetKey(KeyCode.W) then
        wDir = Vector3(0, 0, 1)
    end
    local sDir = Vector3.zero
    if Input.GetKey(KeyCode.S) then
        sDir = Vector3(0, 0, -1)
    end
    local aDir = Vector3.zero
    if Input.GetKey(KeyCode.A) then
        aDir = Vector3(-1, 0, 0)
    end
    local dDir = Vector3.zero
    if Input.GetKey(KeyCode.D) then
        dDir = Vector3(1, 0, 0)
    end
    local keyboardDirection = wDir + sDir + aDir + dDir

    if keyboardDirection.magnitude > 0 then
        self.role.position = self.role.position + keyboardDirection.normalized * 4.0 * deltaTime

        if self.isServerMoving then
            local currTime = Time.time
            if currTime - self.lastSyncPositionTime > 0.125 then
                self.lastSyncPositionTime = currTime

                self.socket:send("C2SActorMoving", {
                    sPos = MakeProtoPos(self.role.position),
                    time = Time.realtimeSinceStartup,
                })
            end
        else
            self.socket:send("C2SActorBeginMove", {
                sPos = MakeProtoPos(self.role.position),
                time = Time.realtimeSinceStartup,
            })
            self.isServerMoving = true
            self.lastSyncPositionTime = Time.time
        end
    else
        if self.isServerMoving then
            self.isServerMoving = false
            self.socket:send("C2SActorStopMove", {
                sPos = MakeProtoPos(self.role.position),
                time = Time.realtimeSinceStartup,
            })
        end
    end
end

--- 收到服务器主角进入视野
---@param args S2CRoleEnterView
function TestClient:S2CRoleEnterView(args)
    Logger.Debug("主角进入视野: %s", args)

    local go = LuaAPI.Instantiate(AssetUtilU5.LoadPrefab("BossGame/Models/Prefabs/x_xds_1", false))
    self.role = go.transform
    self.role.position = MakeVector3(args.pos)

    self.characters[args.objectID] = self.role

    self.socket:send("C2SRoleLoadComplete")
end

--- 其他玩家进入视野
---@param args S2CActorEnterMyView
function TestClient:S2CActorEnterMyView(args)

    local go = LuaAPI.Instantiate(AssetUtilU5.LoadPrefab("BossGame/Models/Prefabs/x_xds_2", false))
    go.transform.position = MakeVector3(args.pos)
    self.characters[args.objectID] = go.transform
end

---退出视野
---@param args S2CCharExitMyView
function TestClient:S2CCharExitMyView(args)
    Logger.Debug("退出视野: %s", args)
    if self.characters[args.objectID] then
        GameObject.Destroy(self.characters[args.objectID].gameObject)
        self.characters[args.objectID] = nil
    end
end

---玩家开始移动
---@param args S2CCharBeginMove
function TestClient:S2CCharBeginMove(args)
    --需要根据目标点做平滑移动，注释：当前直接设置成目标点
    self.characters[args.objectID].position = MakeVector3(args.sPos)

    -- 平滑移动的模块
    -- moveComponent:SetDestination(serverPos, dir)
end

---玩家移动中
---@param args S2CCharMoving
function TestClient:S2CCharMoving(args)
    --需要根据目标点做平滑移动，注释：当前直接设置成目标点
    self.characters[args.objectID].position = MakeVector3(args.sPos)

    -- 平滑移动的模块
    -- moveComponent:SetDestination(serverPos, dir)
end

---玩家停止移动
---@param args S2CCharStopMove
function TestClient:S2CCharStopMove(args)
    --需要根据目标点做平滑移动，注释：当前直接设置成目标点
    self.characters[args.objectID].position = MakeVector3(args.sPos)

    -- 平滑移动的模块
    -- moveComponent:SetDestination(serverPos, dir)
    -- moveComponent:Stop()
end


---回拉玩家位置
---@param args S2CCharCorrectPos
function TestClient:S2CCharCorrectPos(args)
    self.characters[args.objectID].position = MakeVector3(args.sPos)
end

return TestClient