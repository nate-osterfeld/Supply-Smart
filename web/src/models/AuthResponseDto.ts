import type { LocationDto } from "./LocationDto";

export interface AuthResponseDto {
    token: string;
    email: string;
    fullName: string;
    role: string;
    organizationId: number;
    locations: LocationDto[];
}