/// <reference types="node" />
/// <reference types="node" />
import { IncomingMessage } from "http";
import { WebSocket } from "ws";
import { ProfileHelper } from "@spt/helpers/ProfileHelper";
import { IWsNotificationEvent } from "@spt/models/eft/ws/IWsNotificationEvent";
import { IHttpConfig } from "@spt/models/spt/config/IHttpConfig";
import { ILogger } from "@spt/models/spt/utils/ILogger";
import { ConfigServer } from "@spt/servers/ConfigServer";
import { IWebSocketConnectionHandler } from "@spt/servers/ws/IWebSocketConnectionHandler";
import { ISptWebSocketMessageHandler } from "@spt/servers/ws/message/ISptWebSocketMessageHandler";
import { LocalisationService } from "@spt/services/LocalisationService";
import { JsonUtil } from "@spt/utils/JsonUtil";
export declare class SptWebSocketConnectionHandler implements IWebSocketConnectionHandler {
    protected logger: ILogger;
    protected profileHelper: ProfileHelper;
    protected localisationService: LocalisationService;
    protected configServer: ConfigServer;
    protected jsonUtil: JsonUtil;
    protected sptWebSocketMessageHandlers: ISptWebSocketMessageHandler[];
    protected httpConfig: IHttpConfig;
    protected webSockets: Map<string, WebSocket>;
    protected defaultNotification: IWsNotificationEvent;
    protected websocketPingHandler: NodeJS.Timeout | undefined;
    constructor(logger: ILogger, profileHelper: ProfileHelper, localisationService: LocalisationService, configServer: ConfigServer, jsonUtil: JsonUtil, sptWebSocketMessageHandlers: ISptWebSocketMessageHandler[]);
    getSocketId(): string;
    getHookUrl(): string;
    onConnection(ws: WebSocket, req: IncomingMessage): void;
    sendMessage(sessionID: string, output: IWsNotificationEvent): void;
    isConnectionWebSocket(sessionID: string): boolean;
    getSessionWebSocket(sessionID: string): WebSocket;
}
