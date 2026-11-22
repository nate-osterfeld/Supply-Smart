import type { LocationDto } from "./LocationDto";

export interface AuthResponseDto {
    email: string;
    fullName: string;
    role: string;
    organizationId: number;
    locations: LocationDto[];
}
