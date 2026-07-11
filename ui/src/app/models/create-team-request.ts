export interface CreateTeamDto {
    teamName: string;
    version: string;
    administration: string;
}

export interface CreatePlayerDto {
    firstName: string;
    lastName: string;
    email: string;
    phoneNumber: string;
    category: string;
    outfit: string;
    volunteer: boolean;
    acceptMails: boolean;
}

export interface CreateTeamWithPlayersRequest {
    teamDto: CreateTeamDto;
    playerDtos: CreatePlayerDto[];
}

export interface UpdatePlayerDto extends CreatePlayerDto {
    id: number;
}

export interface UpdateTeamWithPlayersRequest {
    teamDto: CreateTeamDto;
    playerDtos: UpdatePlayerDto[];
}
