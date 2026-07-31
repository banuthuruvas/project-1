import api from "./api";

export interface MyInfoPersonData {
  name?: string | null;
  nricFin?: string | null;
  sex?: string | null;
  race?: string | null;
  nationality?: string | null;
  dateOfBirth?: string | null;
  birthCountry?: string | null;
  residentialStatus?: string | null;
  maritalStatus?: string | null;
  email?: string | null;
  mobileNumber?: string | null;
  registeredAddress?: string | null;
  postalCode?: string | null;
  blockNumber?: string | null;
  streetName?: string | null;
  floorNumber?: string | null;
  unitNumber?: string | null;
  highestQualification?: string | null;
  occupation?: string | null;
  employerName?: string | null;
  subject?: string | null;
  verifiedAtUtc?: string | null;
}

export interface TestProfile {
  uinfin: string;
  name: string;
  sex: string;
  race: string;
  nationality: string;
  dob: string;
  email: string;
  mobile: string;
  passType: string;
  residentialStatus: string;
  maritalStatus: string;
  birthCountry: string;
  postalCode: string;
  block: string;
  street: string;
  floor: string;
  unit: string;
}

const myInfoService = {
  async isConfigured(): Promise<boolean> {
    return (await api.get<{ configured: boolean }>("/api/MyInfo/IsConfigured"))
      .data.configured;
  },

  async getAuthorizeUrl(): Promise<string> {
    return (
      await api.get<{ authorizeUrl: string }>("/api/MyInfo/GetAuthorizeUrl")
    ).data.authorizeUrl;
  },

  async callback(authCode: string, state: string): Promise<MyInfoPersonData> {
    return (
      await api.post<MyInfoPersonData>("/api/MyInfo/Callback", {
        authCode,
        state,
      })
    ).data;
  },

  async getTestProfiles(): Promise<TestProfile[]> {
    const response = await fetch(
      `${import.meta.env.BASE_URL}test-profiles.json`,
    );
    return response.json();
  },
};

export default myInfoService;
