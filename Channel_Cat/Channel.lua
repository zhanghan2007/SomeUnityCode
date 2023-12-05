local Http = require "Http"
local json = require "cjson"

Channel = {}

Channel.wxAppId = "wxc7b046e4b3d81c5c"
Channel.qqAppId = "101855722"
-- Channel.iosQQAppId = "1106868354"
Channel.ALIPAY_APPID = "2021001129660896"
--Channel.OfficialChannelId = "80001292"

Channel.shareURL = "http://wxcrzz.74kf.com/share/download.html"

function Channel.init()
    if IsIOS then
        ThirdChannel.InitAppStorePay()
    end
    Channel.agent_id = 0
    print("Channel: ", ThirdChannel.config)
    if ThirdChannel.config then
        local agent = json.decode(ThirdChannel.config)
        Channel.agent_id = agent.id -- 登录ID
        Channel.channel = agent.channel -- 渠道号

        --Channel.channelType = agent.channelType -- 客服端设置渠道类型

        Channel.name = agent.name
        -- Channel.useAgentLoginUI = agent.useAgentLoginUI
        --Channel.showLoginBtn = agent.showLoginBtn or false
        --Channel.officialPay = agent.officialPay or false
        Channel.appId = agent.appId or "0"
        Channel.infullType = agent.infullType or 0
        Channel.ownLogin = agent.ownLogin or false              --是否渠道包
        -- Channel.loginMode = agent.loginMode or 0
        Channel.trusteeship = agent.trusteeship or false        --是否是渠道sdk托管模式，支付、上线时间由sdk控制
        --Channel.openInviteRoom = agent.openInviteRoom or false
        Channel.payMode = agent.payMode or ""
        Channel.shareAppId = agent.wxAppId or Channel.wxAppId
        --Channel.openTest = agent.openTest
        Channel.version = agent.version or ResMgr.appVer
        --Channel.openConsole = agent.openConsole or Application.isEditor
        --Channel.ownAccount = agent.ownAccount
        --Channel.openPay = agent.openPay
        --Channel.hasBBS = agent.hasBBS or false

        --暂时设置渠道号1，一般为读取streamingasset下的channel.json文件
        -- agent.channel = Channel.OfficialChannelId
        --print(string.format("渠道=%s ID=%d Channel=%d Ver=%s", agent.name, agent.id, agent.channel, Channel.version))
    else
        print("ThirdChannel.config:为空")
    end
    -- Channel.agent_id = Channel.OfficialChannelId
    -- Channel.channel = Channel.OfficialChannelId
    Channel.loginCallBack = {}
    Channel.bdingCallBack = {}
end

--渠道是否需要检测上一笔的消单情况
function Channel.NeedCheckBillon()
    if Channel.needCheckPrevBillon == nil then
        --苹果
        if Channel.channel == 80001329 or
                --华为
                Channel.channel == 80001329 then
            Channel.needCheckPrevBillon = false
        else
            Channel.needCheckPrevBillon = true
        end
    end
    return Channel.needCheckPrevBillon
end

function Channel.registerLoginSuccess(eventName, objName)
    local callBack = { func = eventName, obj = objName }
    Channel.loginCallBack[objName] = callBack
end

function Channel.unregisterLoginSuccess(obj)
    Channel.loginCallBack[obj] = nil
end

function Channel.registerBdingSuccess(eventName, objName)
    local callBack = { func = eventName, obj = objName }
    Channel.bdingCallBack[objName] = callBack
end

function Channel.unregisterBdingSuccess(obj)
    Channel.bdingCallBack[obj] = nil
end

function Channel.verifyVersion(ver)
    local lVer = {}
    string.gsub(Channel.version, '[^.]+', function(w) table.insert(lVer, w) end)

    local sVer = {}
    string.gsub(ver, '[^.]+', function(w) table.insert(sVer, w) end)

    if #lVer ~= 3 or #sVer ~= 3 then
        return false
    end

    if lVer[1] > sVer[1] then
        return true
    elseif lVer[1] < sVer[1] then
        return false
    end

    if lVer[2] > sVer[2] then
        return true
    elseif lVer[2] < sVer[2] then
        return false
    end

    return lVer[3] >= sVer[3]
end

function Channel.sdk_login(token, extend, loginId)
    local deviceId = UnityUtil.GetDeviceId()
    Http.post(Game.loginUrl, {
        func = "sdk_login_new",
        extend = extend,
        token = token,
        appname = Application.identifier,
        version = Game.version,
        device = deviceId,
        processorType = SystemInfo.processorType,
        processorFrequency = SystemInfo.processorFrequency,
        deviceModel = SystemInfo.deviceModel,
        graphicsDeviceName = SystemInfo.graphicsDeviceName,
        mac = UnityUtil.GetMacAddress(),
        agent_id = loginId,
        channel = Channel.channel,
        age = Channel.GetPlayerAgeFormSDK(),
    },
            function(args)
                if Game.HttpErrorBeforeLogin(args) then return end

                if args.result == 0 then
                    Ex_Channel.OnChannelLoginSuccess(args)
                    Ex_Channel.SetShiMingAge(args.age)
                else
                    if not args.msg then
                        --小米用 没有实名
                        if args.result == -99 then
                            Channel.GoToGetVerified()
                        elseif args.result == -372 then
                            MsgBox.show(args.info)
                        elseif args.result == -373 then
                            local s = string.format(args.info, Channel.secondToData(args.frozontime))
                            MsgBox.show(s)
                        end
                    else
                        MsgBox.show(args.msg)
                        print("登录失败，" .. args.msg .. "尝试更新")
                    end
                end
                Channel.loginSuccess(args)
            end)
end

function Channel.getAgentId()
    return Channel.agent_id
end

function Channel.loginSuccess(args)
    table.print("Channel.loginSuccess登录成功回调", args)
    if Channel.loginCallBack then
        for _, v in pairs(Channel.loginCallBack) do
            v.func(v.obj, args)
        end
        -- Channel.loginCallBack = {}
    end

end

function Channel.bdingSuccess(args)
    table.print("Channel.bdingSuccess绑定成功回调", args)
    if Channel.bdingCallBack then
        for _, v in pairs(Channel.bdingCallBack) do
            v.func(v.obj, args)
        end
        -- Channel.bdingCallBack = {}
    end
end

--渠道sdk获取实名认证年龄
function Channel.onVerifiedSucceed(age)
    print("lua-->", "onVerifiedSucceed", age)
    Channel.playerAge = tonumber(age)
    Channel.SendVerifiedInfoToServer()
end

function Channel.onVerifiedFailed(age)
    print("lua-->", "onVerifiedFailed", age)
    Channel.playerAge = tonumber(age)
    Channel.SendVerifiedInfoToServer()
end

function Channel.GetPlayerAgeFormSDK()
    Channel.playerAge = Channel.playerAge or Ex_Channel.GetShiMingAge()
    if not Channel.playerAge then
        print("意外：从sdk方未拿到玩家年龄")
    end
    return Channel.playerAge
end

function Channel.GoToGetVerified(resToServer)
    Channel.resToServer = resToServer
    if Ex_Channel.GetShiMingAge() then
        --再拓展
        return
    else
        ThirdChannel.GetVerifiedInfo()
    end
end

function Channel.SendVerifiedInfoToServer()
    if Channel.resToServer then
        if EntryMgrInst and EntryMgrInst.socket then
            if not Channel.playerAge or Channel.playerAge == 0 then
                Channel.resToServer = nil
                return
            end
            EntryMgrInst:CallLock("C2SSetPlayerAge", { age = Channel.playerAge or 0 }, function(args)
                table.print("向服务器发送更改实名", args)
                Channel.resToServer = nil
            end)
        end
    end
end

-- 提交数据给代理商
function Channel.submitRoleData(type)
    local data = {
        type = type,
        roleId = tostring(Game.playerInfo.uid),
        resV = ResMgr.resVer,
        appV = ResMgr.appVer,
        serverType = PlayerPrefs.GetInt("serverType", -1) .. "",
        roleName = Game.playerInfo.rolename,
        roleLevel = tostring(Game.playerInfo.lv),
        roleCTime = tostring(Game.playerInfo.createtime),
        zoneId = "1",
        zoneName = "一区",
        guildName = "无",
        guildRoleJob = "0", -- 会长1，其他0
        vip = "0",
        balance = tostring(Game.playerInfo.crystal),
        sex = Game.playerInfo.sex
    }
    ThirdChannel.SubmitRoleData(json.encode(data))
end

function Channel.isUseWXShare()
    return PlayerPrefs.GetInt("LoginType") ~= 2
end

local reqLoginFuncs = {}

function Channel.wxLogin(params, agent_id)
    Channel.shareAppId = Channel.wxAppId
    print("Channel.wxLogin", Game.loginUrl, params[1], Channel.channel, Game.version)
    Http.post(Game.loginUrl, {
        func = "wx_access_token",
        code = params[1],
        version = Game.version,
        device = UnityUtil.GetDeviceId(),
        processorType = SystemInfo.processorType,
        processorFrequency = SystemInfo.processorFrequency,
        deviceModel = SystemInfo.deviceModel,
        graphicsDeviceName = SystemInfo.graphicsDeviceName,
        mac = UnityUtil.GetMacAddress(),
        agent_id = Channel.getAgentId(),
        channel = Channel.channel
    },
            function(args)
                if Game.HttpErrorBeforeLogin(args) then return end

                if args.result == 0 then
                    PlayerPrefs.SetInt("LoginType", 1)
                    PlayerPrefs.SetString("WXToken", args.refresh_token)
                    Channel.openId = args.openid

                    -- Lobby.connectLobby(args)

                    print("wxLogin-->登录成功")
                    MsgBox.show(3013)
                else
                    print("wxLogin-->登录失败")
                    table.print("wx_access_token", args)
                    -- MsgBox.show(3014)
                    if args.result == 50 then
                        MsgBox.show(args.msg)
                    elseif args.result == -372 then
                        MsgBox.show(args.info)
                        return
                    elseif args.result == -373 then
                        local s = string.format(args.info, Channel.secondToData(args.frozontime))
                        MsgBox.show(s)
                        return
                    else
                        MsgBox.show(3014)
                    end
                end
                Channel.loginSuccess(args)
            end)
end

function Channel.wxLoginNoFirst()
    print("Channel.wxLoginNoFirst", Game.loginUrl, PlayerPrefs.GetString("WXToken"), Channel.channel, Game.version)
    Http.post(Game.loginUrl, {
        func = "wx_refresh_token",
        refresh_token = PlayerPrefs.GetString("WXToken"),
        version = Game.version,
        device = UnityUtil.GetDeviceId(),
        processorType = SystemInfo.processorType,
        processorFrequency = SystemInfo.processorFrequency,
        deviceModel = SystemInfo.deviceModel,
        graphicsDeviceName = SystemInfo.graphicsDeviceName,
        mac = UnityUtil.GetMacAddress(),
        agent_id = Channel.getAgentId(),
        channel = Channel.channel
    },
            function(args)
                if Game.HttpErrorBeforeLogin(args) then return end

                if args.result == 1004 then
                    Channel.wxOnLoginSucceedCallBack.wxLogin = Channel.wxLogin
                    PlayerPrefs.SetInt("OpenStartBtn", 1)
                    ThirdChannel.Login()
                else
                    if args.result == 0 then
                        PlayerPrefs.SetInt("LoginType", 1)
                        Channel.openId = args.openid

                        print("wxLoginNoFirst-->再次登录成功")
                        MsgBox.show(3013)
                        -- Lobby.connectLobby(args)
                        Ex_Channel.OnChannelLoginSuccess(args)

                    else
                        print("wxLoginNoFirst-->再次登录失败")
                        table.print("wx_access_token", args)
                        -- MsgBox.show(3014)
                        if args.result == 50 then
                            MsgBox.show(args.msg)
                        elseif args.result == -372 then
                            MsgBox.show(args.info)
                            return
                        elseif args.result == -373 then
                            local s = string.format(args.info, Channel.secondToData(args.frozontime))
                            MsgBox.show(s)
                            return
                        else
                            MsgBox.show(3014)
                        end
                    end
                    Channel.loginSuccess(args)
                end
            end)
end

function Channel.wxBding(params)
    print("Channel.wxBding", params[1])
    if EntryMgrInst then
        EntryMgrInst:CallLock("C2SBdingWX", { code = params[1] }, function(args)
            if args.result == 0 then
                table.print("微信绑定成功", args)
                MsgBox.show(3015)
                PlayerPrefs.SetInt("LoginType", 1)
                PlayerPrefs.SetString("WXToken", args.refresh_token)
            else
                table.print("微信绑定失败", args)
                -- MsgBox.show(3016)
                if args.result == 50 then
                    MsgBox.show(args.msg)
                    return
                else
                    MsgBox.show(args.result)
                end
            end
            Channel.bdingSuccess(args)
        end)
    end
end

--登录之后获取安卓端的扩展参数
function Channel.onExtraDataSuncced(str)
    Channel.extraData = str
end

-- 登录回调，授权成功
Channel.wxOnLoginSucceedCallBack = {}
function Channel.onLoginSucceed(result)
    local agent_id = Channel.getAgentId()
    print(string.format("Channel.onLoginSucceed agent_id=%s result=%s", tostring(agent_id), tostring(result)))

    -- 在显示登录界面前不要调用登录
    -- if not Channel.loginUI then
    --     return
    -- end

    -- if Channel.ownLogin then
    --     local params = string.split(result, " ")
    --     sdk_login(params[1], params[2], agent_id)
    -- else
    --     local func = reqLoginFuncs[agent_id]
    --     if func then
    --         func(result)
    --     else
    --         local params = string.split(result, " ")
    --         Channel.wxLogin(params)
    --     end
    -- end

    if Channel.ownLogin then
        local params = string.split(result, " ")
        --第一个参数token
        -- sdk_login(params[1], params[2], agent_id)
        Channel.channelParams = params
        table.print("渠道参数：", params)

        --如果是渠道登录，需要通过渠道sdk获取实名认证的年龄来控制上限时间
        --小米特殊处理
        if not Channel.trusteeship and Channel.name ~= "xiaomi" then
            Channel.GoToGetVerified()
        end
    else
        local params = string.split(result, " ")
        for _, func in pairs(Channel.wxOnLoginSucceedCallBack) do
            func(params)
        end
    end
    -- Channel.wxOnLoginSucceedCallBack = {}
end

function Channel.onLoginFailed(result)
    print("Channel.onLoginSucceed result=" .. tostring(result))
    if Channel.loginUI then
        Channel.loginUI:showButton(true)
    end
end

function Channel.onLogout()
    -- 战斗中不退出
    if not BattleInst then
        Channel.submitRoleData(4)
        Lobby.switchLaunch(true)
    end
end

-- 支付成功
function Channel.onWXPaySucceed(orderId)
    --新版sdk不带订单号回来
    print("Channel.onWXPaySucceed orderId=" .. orderId)
    if EntryMgrInst then
        GameMgrInst:PayWaitWin()
        EntryMgrInst:Send("C2SPaySuccess", { billno = orderId })
    end
    -- Lobby.send("C2SPaySuccess", {billno=orderId})
end

-- 支付成功
function Channel.onHuaweiPaySucceed(result)
    print("Channel.onHuaweiPaySucceed result=" .. result)
    Ex_Channel.C2SGetPayResult(result)
    if GameMgrInst then
        GameMgrInst:PayWaitWin()
    end
    -- Lobby.send("C2SPaySuccess", {billno=orderId})
end

-- 先销单再进行新的购买
function Channel.onProductOwned()
    print("Channel.onProductOwned")
    Ex_Channel.CheckUncomptedOrders()
    if GameMgrInst then
        --协调老代码
        GameMgrInst:PayResult(false)
        GameMgrInst:PayWaitWin()
        MsgBox.show(4228)
    end
    -- Lobby.send("C2SPaySuccess", {billno=orderId})
end

-- 苹果内购支付成功
function Channel.onAppStorePaySucceed(result)
    local params = string.split(result, " ")
    print("Channel.onAppStorePaySucceed billno=" .. params[2])

    if string.find(params[2], "time") then
        -- 获取不到订单，先请求下单后提交数据
        EntryMgrInst.socket:calllock("C2SGetBillno", {
            ID = data.ID,
            agent_id = Channel.getAgentId(),
            appId = Channel.appId,
            infullType = Channel.infullType
        }, function(args)

            print("C2SGetBillno ", args.result, args.billno, args.extraData, data.iOSProductId)
            if args.result == 0 then
                Lobby.call("C2SIosPayData", { billno = args.billno, data = params[1] }, function(args)
                    print("C2SIosPayData result", args.result, args.billno)
                    if args.result == 0 then
                        ThirdChannel.OnPaySuccess(args.billno)
                    end
                end)
            else
                MsgBox.show(args.result)
            end
        end)
    else
        Lobby.call("C2SIosPayData", { billno = params[2], data = params[1] }, function(args)
            print("C2SIosPayData result", args.result, args.billno)
            if args.result == 0 then
                ThirdChannel.OnPaySuccess(args.billno)
            end
        end)
    end
end

-- 支付失败
Channel.payFailedCallBack = {}
function Channel.onWXPayFailed(orderId)
    print("Channel.onWXPayFailed orderId=", orderId)
    for _, cb in pairs(Channel.payFailedCallBack) do
        cb.func(cb.obj)
    end
    -- MsgBox.show(-345)
end

-- 微信分享成功
function Channel.onWXShareSucceed(openid)
    if openid == "" and Channel.openId then
        openid = Channel.openId -- ios和应用宝分享回调无法直接获取openid
    end

    print("Channel.onWXShareSucceed openid=" .. tostring(openid) .. ",shareAppId=" .. tostring(Channel.shareAppId)
            .. "Game.RedPacketCallBack=" .. tostring(Game.RedPacketCallBack)
            .. "Game.ShareWXTaskCallBack=" .. tostring(Game.ShareWXTaskCallBack))

    local channel = 1
    if Channel.isUseWXShare() then
        channel = 1
    else
        channel = 2
    end
    if Game.RedPacketCallBack then
        Game.RedPacketCallBack(openid, Channel.shareAppId, channel)
    end

    if Game.ShareWXTaskCallBack then
        Game.ShareWXTaskCallBack()
    end

    if Game.WXShareEventInfo then
        EventManager.notify(Game.WXShareEventInfo.name, Game.WXShareEventInfo.id, openid, Channel.shareAppId, channel)
    end
end

-- 微信分享失败
function Channel.onWXShareFailed(args)
    print("Channel.onWXShareFailed")

    if Game.RedPacketCallBack then
        Game.RedPacketCallBack = nil
    end

    if Game.ShareWXTaskCallBack then
        Game.ShareWXTaskCallBack = nil
    end

    if Game.WXShareEventInfo then
        Game.WXShareEventInfo = nil
    end
end

-- QQ 登录成功
Channel.qqOnLoginSucceedCallBack = {}
function Channel.QQ_onLoginSucceed(result)
    print("Channel.QQ_onLoginSucceed result=" .. result)

    local params = string.split(result, " ")
    if #params < 2 then
        print("QQ_onLoginSucceed 参数错误！")
        return
    end

    local params = string.split(result, " ")
    for _, func in pairs(Channel.qqOnLoginSucceedCallBack) do
        func(params)
    end
    -- Channel.qqOnLoginSucceedCallBack = {}
end

--调用qq官方sdk登录qq，用以登录已有账号和切换账号
function Channel.qqLogin(params)
    table.print("Channel.qqLogin", params)
    local agent_id = Channel.getAgentId()
    local func = "qq_login"
    if Application.platform == RuntimePlatform.IPhonePlayer then
        func = "qq_login_ios"
        Channel.shareAppId = Channel.iosQQAppId
    else
        Channel.shareAppId = Channel.qqAppId
    end

    Channel.openId = params[1]

    Http.post(Game.loginUrl, { func = func,
                               openid = params[1],
                               access_token = params[2],
                               version = Game.version,
                               device = UnityUtil.GetDeviceId(),
                               processorType = SystemInfo.processorType,
                               processorFrequency = SystemInfo.processorFrequency,
                               deviceModel = SystemInfo.deviceModel,
                               graphicsDeviceName = SystemInfo.graphicsDeviceName,
                               mac = UnityUtil.GetMacAddress(),
                               agent_id = agent_id,
                               channel = Channel.channel
    },
            function(args)
                if Game.HttpErrorBeforeLogin(args) then return end

                if args.result == 0 then
                    table.print("qq登录成功", args)
                    print("qq设置PlayerPrefs", params[1], params[2])
                    PlayerPrefs.SetInt("LoginType", 2)
                    PlayerPrefs.SetString("QQopenid", params[1])
                    PlayerPrefs.SetString("QQaccess_token", params[2])

                    print("qqLogin-->登录成功")
                    MsgBox.show(3013)
                else
                    print("qqLogin-->登录失败")
                    table.print("qq_login", args)
                    -- MsgBox.show(3014)
                    if args.result == 50 then
                        MsgBox.show(args.msg)
                    elseif args.result == -372 then
                        MsgBox.show(args.info)
                        return
                    elseif args.result == -373 then
                        local s = string.format(args.info, Channel.secondToData(args.frozontime))
                        MsgBox.show(s)
                        return
                    else
                        MsgBox.show(3014)
                    end
                end
                Channel.loginSuccess(args)
            end)
end

--在游戏中第一次绑定qq登录
function Channel.qqBding(params)
    table.print("Channel.qqBding", params)
    if EntryMgrInst then
        EntryMgrInst:CallLock("C2SBdingQQ", { access_token = params[2], openid = params[1] }, function(args)
            if args.result == 0 then
                table.print("qq绑定成功", args)
                print("qq设置PlayerPrefs", params[1], params[2])
                MsgBox.show(3015)
                PlayerPrefs.SetInt("LoginType", 2)
                PlayerPrefs.SetString("QQopenid", params[1])
                PlayerPrefs.SetString("QQaccess_token", params[2])
            else
                table.print("qq绑定失败", args)
                -- MsgBox.show(3016)
                if args.result == 50 then
                    MsgBox.show(args.msg)
                else
                    MsgBox.show(args.result)
                end
            end
            Channel.bdingSuccess(args)
        end)
    end
end

--上次登录方式为qq时，下次进入游戏直接登录
function Channel.qqLoginNoFirst()
    print("Channel.qqLoginNoFirst", Game.loginUrl, PlayerPrefs.GetString("QQopenid"),
            PlayerPrefs.GetString("QQaccess_token"), Channel.channel, Game.version)

    local agent_id = Channel.getAgentId()
    local func = "qq_login"
    if Application.platform == RuntimePlatform.IPhonePlayer then
        func = "qq_login_ios"
        Channel.shareAppId = Channel.iosQQAppId
    else
        Channel.shareAppId = Channel.qqAppId
    end

    Channel.openId = PlayerPrefs.GetString("QQopenid")

    local qqopenid = Channel.openId
    local qqaccess_token = PlayerPrefs.GetString("QQaccess_token")

    Http.post(Game.loginUrl, { func = func,
                               openid = qqopenid,
                               access_token = qqaccess_token,
                               version = Game.version,
                               device = UnityUtil.GetDeviceId(),
                               processorType = SystemInfo.processorType,
                               processorFrequency = SystemInfo.processorFrequency,
                               deviceModel = SystemInfo.deviceModel,
                               graphicsDeviceName = SystemInfo.graphicsDeviceName,
                               mac = UnityUtil.GetMacAddress(),
                               agent_id = agent_id,
                               channel = Channel.channel
    },
            function(args)
                if Game.HttpErrorBeforeLogin(args) then return end

                --过期给个专门result
                if args.result == 1004 then
                    Channel.qqOnLoginSucceedCallBack.qqLogin = Channel.qqLogin
                    PlayerPrefs.SetInt("OpenStartBtn", 1)
                    ThirdChannel.QQLogin()
                else
                    if args.result == 0 then
                        table.print("qq再次登录成功", args)
                        -- print("qq设置PlayerPrefs",params[1],params[2])
                        PlayerPrefs.SetInt("LoginType", 2)
                        -- PlayerPrefs.SetString("QQ_openid", qqopenid)
                        -- PlayerPrefs.SetString("QQ_access_token", qqaccess_token)

                        print("qqLoginNoFirst-->登录成功")
                        MsgBox.show(3013)
                        Ex_Channel.OnChannelLoginSuccess(args)

                    else
                        print("qqLoginNoFirst-->登录失败")
                        table.print("qq_login", args)
                        -- MsgBox.show(3014)
                        if args.result == 50 then
                            MsgBox.show(args.msg)
                        elseif args.result == -372 then
                            MsgBox.show(args.info)
                            return
                        elseif args.result == -373 then
                            local s = string.format(args.info, Channel.secondToData(args.frozontime))
                            MsgBox.show(s)
                            return
                        else
                            MsgBox.show(3014)
                        end
                    end
                    Channel.loginSuccess(args)
                end
            end)
end

--苹果登录成功
Channel.appleOnLoginSucceedCallBack = {}
function Channel.Apple_onLoginSucceed(result)
    print("Channel.Apple_onLoginSucceed result=" .. result)

    local params = string.split(result, " ")
    if #params < 2 then
        print("Apple_onLoginSucceed 参数错误！")
        return
    end

    for _, func in pairs(Channel.appleOnLoginSucceedCallBack) do
        func(params)
    end
    -- Channel.appleOnLoginSucceedCallBack = {}
end

function Channel.Apple_onLoginFailed(result)
    print("Channel.Apple_onLoginFailed result=" .. result)
    if Channel.loginUI then
        Channel.loginUI:showButton(true)
    end
end

--调用apple官方sdk登录qq，用以登录已有账号和切换账号
function Channel.appleLogin(params)
    table.print("Channel.appleLogin", params)
    local agent_id = Channel.getAgentId()
    local func = "apple_login"
    --if Application.platform == RuntimePlatform.IPhonePlayer then
    --    func = "qq_login_ios"
    --    Channel.shareAppId = Channel.iosQQAppId
    --else
    --    Channel.shareAppId = Channel.qqAppId
    --end

    Channel.openId = "" -- params[2]

    --多次验证会被苹果服务器T
    --先用老token试 不行换新token
    local oldToken = PlayerPrefs.GetString("AppleidentityToken", "")

    Http.post(Game.loginUrl, { func = func,
                               userID = params[1],
                               identityToken = params[2],
                               old_identityToken = oldToken;
                               version = Game.version,
                               device = UnityUtil.GetDeviceId(),
                               processorType = SystemInfo.processorType,
                               processorFrequency = SystemInfo.processorFrequency,
                               deviceModel = SystemInfo.deviceModel,
                               graphicsDeviceName = SystemInfo.graphicsDeviceName,
                               mac = UnityUtil.GetMacAddress(),
                               agent_id = agent_id,
                               channel = Channel.channel
    },
            function(args)
                if Game.HttpErrorBeforeLogin(args) then return end

                if args.result == 0 then
                    table.print("苹果登录成功", args)
                    print("苹果设置PlayerPrefs", params[1], args.identityToken)
                    PlayerPrefs.SetInt("LoginType", 4)
                    PlayerPrefs.SetString("AppleuserID", params[1])
                    PlayerPrefs.SetString("AppleidentityToken", args.identityToken)

                    MsgBox.show(3013)
                else
                    print("苹果登录失败")
                    table.print("apple_login", args)
                    -- MsgBox.show(3014)
                    if args.result == 50 then
                        MsgBox.show(args.msg)
                    elseif args.result == -372 then
                        MsgBox.show(args.info)
                        return
                    elseif args.result == -373 then
                        local s = string.format(args.info, Channel.secondToData(args.frozontime))
                        MsgBox.show(s)
                        return
                    else
                        MsgBox.show(3014)
                    end
                end
                Channel.loginSuccess(args)
            end)
end

--在游戏中第一次绑定apple登录
function Channel.appleBding(params)
    table.print("Channel.appleBding", params)
    if EntryMgrInst then
        EntryMgrInst:CallLock("C2SBdingApple", { userID = params[1], identityToken = params[2] }, function(args)
            if args.result == 0 then
                table.print("苹果绑定成功", args)
                MsgBox.show(3015)
                PlayerPrefs.SetInt("LoginType", 4)
                PlayerPrefs.SetString("AppleuserID", params[1])
                PlayerPrefs.SetString("AppleidentityToken", params[2])
            else
                table.print("苹果绑定失败", args)
                -- MsgBox.show(3016)
                if args.result == 50 then
                    MsgBox.show(args.msg)
                else
                    MsgBox.show(args.result)
                end
            end
            Channel.bdingSuccess(args)
        end)
    end
end

--上次登录方式为qq时，下次进入游戏直接登录
function Channel.appleLoginNoFirst()
    print("Channel.appleLoginNoFirst", Game.loginUrl, PlayerPrefs.GetString("AppleuserID"),
            PlayerPrefs.GetString("AppleidentityToken"), Channel.channel, Game.version)

    local agent_id = Channel.getAgentId()
    local func = "apple_login"
    --if Application.platform == RuntimePlatform.IPhonePlayer then
    --    func = "qq_login_ios"
    --    Channel.shareAppId = Channel.iosQQAppId
    --else
    --    Channel.shareAppId = Channel.qqAppId
    --end

    Channel.openId = ""-- PlayerPrefs.GetString("QQopenid")

    local appleuserID = PlayerPrefs.GetString("AppleuserID")
    local appleidentityToken = PlayerPrefs.GetString("AppleidentityToken")

    Http.post(Game.loginUrl, { func = func,
                               userID = appleuserID,
                               old_identityToken = appleidentityToken,
                               version = Game.version,
                               device = UnityUtil.GetDeviceId(),
                               processorType = SystemInfo.processorType,
                               processorFrequency = SystemInfo.processorFrequency,
                               deviceModel = SystemInfo.deviceModel,
                               graphicsDeviceName = SystemInfo.graphicsDeviceName,
                               mac = UnityUtil.GetMacAddress(),
                               agent_id = agent_id,
                               channel = Channel.channel
    },
            function(args)
                if Game.HttpErrorBeforeLogin(args) then return end

                --过期给个专门result
                if args.result == 1004 then
                    Channel.appleOnLoginSucceedCallBack.appleLogin = Channel.appleLogin
                    PlayerPrefs.SetInt("OpenStartBtn", 1)
                    ThirdChannel.Apple_Login()
                else
                    if args.result == 0 then
                        table.print("苹果再次登录成功", args)
                        -- print("qq设置PlayerPrefs",params[1],params[2])
                        PlayerPrefs.SetInt("LoginType", 4)
                        -- PlayerPrefs.SetString("QQ_openid", qqopenid)
                        -- PlayerPrefs.SetString("QQ_access_token", qqaccess_token)
                        Ex_Channel.OnChannelLoginSuccess(args)

                        MsgBox.show(3013)
                    else
                        print("苹果再次登录成功-->登录失败")
                        table.print("apple_login", args)
                        -- MsgBox.show(3014)
                        if args.result == 50 then
                            MsgBox.show(args.msg)
                        elseif args.result == -372 then
                            MsgBox.show(args.info)
                            return
                        elseif args.result == -373 then
                            local s = string.format(args.info, Channel.secondToData(args.frozontime))
                            MsgBox.show(s)
                            return
                        else
                            MsgBox.show(3014)
                        end
                    end
                    Channel.loginSuccess(args)
                end
            end)
end

reqLoginFuncs[57] = Channel.ysdkLogin
reqLoginFuncs[58] = Channel.ysdkLogin
reqLoginFuncs[107] = Channel.rrgameLogin

-- 获取微信openid再去分享
Channel.onGetWXCode = function(result)
    print("Channel.onGetWXCode result=" .. result)
    local params = string.split(result, " ")

    Http.post(Game.loginUrl, { func = "get_wxopenid",
                               wxAppId = params[1],
                               code = params[2],
                               channel = Channel.channel
    },
            function(args)
                if Game.HttpErrorBeforeLogin(args) then return end
                if args.result == 0 then
                    print("wxopenid=" .. args.openid)
                    Channel.openId = args.openid

                    Game.shareImage(1) -- 朋友圈
                else
                    if not args.msg then
                        print("get_wxopenid---", args.result)
                        -- MsgBox.show(args.result)
                    else
                        MsgBox.show(args.msg)
                    end
                end
            end)
end

--电话模块
function Channel.phoneBding(phoneNum, phonePwd, bdtype)
    print("Channel.phoneBding-->", phoneNum, phonePwd)
    PlayerPrefs.SetInt("LoginType", 3)
    PlayerPrefs.SetString("PhoneNum", phoneNum)
    PlayerPrefs.SetString("PhonePwd", phonePwd)
    MsgBox.show(bdtype and 3015 or 3024)
    local args = {}
    args.result = 0
    Channel.bdingSuccess(args)
end

function Channel.phoneLogin()
    local Num = PlayerPrefs.GetString("PhoneNum")
    local Pwd = PlayerPrefs.GetString("PhonePwd")
    print("Channel.phoneLogin-->", Num, Pwd)
    Http.post(Game.loginUrl, {
        func = "phonelogin",
        phoneNum = Num,
        phonePwd = Pwd,
        version = Game.version,
        device = UnityUtil.GetDeviceId(),
        processorType = SystemInfo.processorType,
        processorFrequency = SystemInfo.processorFrequency,
        deviceModel = SystemInfo.deviceModel,
        graphicsDeviceName = SystemInfo.graphicsDeviceName,
        mac = UnityUtil.GetMacAddress(),
        agent_id = Channel.getAgentId(),
        channel = Channel.channel
    },
            function(args)
                if Game.HttpErrorBeforeLogin(args) then return end

                if args.result == 0 then
                    PlayerPrefs.SetInt("LoginType", 3)
                    print(PlayerPrefs.HasKey("LoginType"))
                    print(PlayerPrefs.GetInt("LoginType"))
                    print("电话登录成功")
                    MsgBox.show(3013)
                    Ex_Channel.OnChannelLoginSuccess(args)

                else
                    table.print("电话登录失败", args)
                    -- MsgBox.show(3014)
                    if args.result == 50 then
                        MsgBox.show(args.msg)
                        return
                    elseif args.result == -372 then
                        MsgBox.show(args.info)
                        return
                    elseif args.result == -373 then
                        local s = string.format(args.info, Channel.secondToData(args.frozontime))
                        MsgBox.show(s)
                        return
                    else
                        MsgBox.show(3014)
                    end
                end
                Channel.loginSuccess(args)
                if args.result == 1004 then
                    require("Login_Account_LoginWin").new(args.uid)
                end
            end)
end
--电话模块结束

function Channel.clearAccount()
    print("清除账号数据")
    PlayerPrefs.DeleteKey("LoginType")
    PlayerPrefs.DeleteKey("WXToken")
    PlayerPrefs.DeleteKey("QQopenid")
    PlayerPrefs.DeleteKey("QQaccess_token")
    PlayerPrefs.DeleteKey("PhoneNum")
    PlayerPrefs.DeleteKey("PhonePwd")
end

function Channel.secondToData(second)
    local s = ""
    if second < 0 then return s end
    local t = FormatSecond2Date(second)
    local day = t.year * 365 + t.month * 30 + t.day
    if day > 0 then
        s = day .. "天"
    else
        if t.hour > 0 then
            s = t.hour .. "小时"
        end
        if t.minute > 0 then
            s = s .. t.minute .. "分"
        end
        if t.second > 0 then
            s = s .. t.second .. "秒"
        end
    end
    return s
end

function Channel.CheckSendToBoke()
    if not SDKHelper.IsBokeChannel() then
        return
    end
    local billons = SDKHelper.GetCacheBillon()
    table.print("billons", billons)
    local t = {}
    if billons and table.size(billons) > 0 then
        for _, v in pairs(billons) do
            table.insert(t, v.billno)
        end

        local sendTimes = math.ceil(#t / 10)
        local sendt = {}
        for idx, v in ipairs(t) do
            local i = math.ceil(idx / 10)
            if not sendt[i] then
                sendt[i] = {}
            end
            table.insert(sendt[i], v)
        end

        local co = coroutine.create(function()
            for _, v in ipairs(sendt) do
                Http.post(Game.loginUrl, {
                    func = "checkpayorder",
                    orders = json.encode(v)
                },
                        function(args)
                            if args.result == 0 then
                                if #args.validorders > 0 then
                                    for _, v in ipairs(args.validorders) do
                                        local billonData = billons[v.billno]
                                        if billonData then
                                            print("登录时向波克后台提交数据：", billonData.billno)
                                            SDKHelper.BokePay(billonData.uid, v.price, billonData.infullType or 0, billonData.billno)
                                            local str = string.format("发送订单到波克买量后台,id:%s,价格：%s,充值类型：%s,订单号：%s", billonData.uid, v.price, (billonData.infullType or 0), billonData.billno)
                                            Game.SendServerLog(str, Game.ServerLogType.PayToBoke)
                                        end
                                    end
                                end
                            end
                        end)
                Yield(WaitForSeconds(0.5))
            end
        end)
        coroutine.resume(co)
    end
end

return Channel