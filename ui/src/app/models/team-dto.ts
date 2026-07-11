export interface PlayerDto {
    id: number;
    firstName: string;
    lastName: string;
    email: string;
    phoneNumber: string;
    category: string;
    outfit: string;
    volunteer: boolean;
    acceptMails: boolean;
}

export interface TeamDto {
    id: number;
    name: string;
    version: string;
    category: string;
    administration: string;
    isPaid: boolean;
    hasAccount: boolean;
    accountVerified: boolean;
    players: PlayerDto[];
}
