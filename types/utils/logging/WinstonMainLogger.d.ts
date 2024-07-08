import { IAsyncQueue } from "@spt/models/spt/utils/IAsyncQueue";
import { AbstractWinstonLogger } from "@spt/utils/logging/AbstractWinstonLogger";
export declare class WinstonMainLogger extends AbstractWinstonLogger {
    protected asyncQueue: IAsyncQueue;
    constructor(asyncQueue: IAsyncQueue);
    protected isLogExceptions(): boolean;
    protected isLogToFile(): boolean;
    protected isLogToConsole(): boolean;
    protected getFilePath(): string;
    protected getFileName(): string;
}
