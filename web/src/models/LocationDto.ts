import type { RoomDto } from "./RoomDto";

export interface LocationDto {
    id: number;
    name: string;
    isPrimary: boolean;
    rooms: RoomDto[];
}