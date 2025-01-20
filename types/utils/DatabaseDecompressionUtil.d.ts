import { ILogger } from "@spt/models/spt/utils/ILogger";
export declare class DatabaseDecompressionUtil {
    protected logger: ILogger;
    private compressedDir;
    private assetsDir;
    private compiled;
    constructor(logger: ILogger);
    /**
     * Checks if the application is running in a compiled environment. A simple check is done to see if the relative
     * assets directory exists. If it does not, the application is assumed to be running in a compiled environment. All
     * relative asset paths are different within a compiled environment, so this simple check is sufficient.
     */
    private isCompiled;
    /**
     * Initializes the database compression utility.
     *
     * This method will decompress all 7-zip archives within the compressed database directory. The decompressed files
     * are placed in their respective directories based on the name and location of the compressed file.
     */
    initialize(): Promise<void>;
    /**
     * Retrieves a list of all 7-zip archives within the compressed database directory.
     */
    private getCompressedFiles;
    /**
     * Processes a compressed file by checking if the target directory is empty, and if so, decompressing the file into
     * the target directory.
     */
    private processCompressedFile;
    /**
     * Checks if a directory exists and is empty.
     */
    private isDirectoryEmpty;
    /**
     * Decompresses a 7-zip archive to the target directory.
     */
    private decompressFile;
}
