import type { ContainerDto } from "./ContainerDto";

export interface RoomDto {
    id: number;
    name: string;
    containers: ContainerDto[];
}