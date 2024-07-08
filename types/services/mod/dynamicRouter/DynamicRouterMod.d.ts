import { DynamicRouter, RouteAction } from "@spt/di/Router";
export declare class DynamicRouterMod extends DynamicRouter {
    private topLevelRoute;
    constructor(routes: RouteAction[], topLevelRoute: string);
    getTopLevelRoute(): string;
}
