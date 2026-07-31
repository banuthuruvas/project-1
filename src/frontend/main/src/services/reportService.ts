import api from "./api";

export interface ReportTypeDefinition {
  id: string;
  name: string;
  description: string;
  category: string;
  icon: string;
  filters: ReportFilter[];
}

export interface ReportFilter {
  name: string;
  label: string;
  type: string;
  options?: string[];
}

export interface ReportRequest {
  reportType: string;
  status?: string;
  dateFrom?: string | null;
  dateTo?: string | null;
  vendorId?: number | null;
  category?: string;
  userId?: string;
}

const reportService = {
  async getReportTypes(): Promise<ReportTypeDefinition[]> {
    const res = await api.get("/api/Report/types");
    return res.data;
  },

  async generatePreview(
    reportType: string,
    request: ReportRequest,
  ): Promise<string> {
    const res = await api.post(
      "/api/Report/preview",
      { ...request, reportType },
      { responseType: "text" },
    );
    return res.data;
  },

  async downloadPdf(reportType: string, request: ReportRequest): Promise<void> {
    const res = await api.post(
      "/api/Report/download",
      { ...request, reportType },
      { responseType: "blob" },
    );
    const url = URL.createObjectURL(res.data);
    const a = document.createElement("a");
    a.href = url;
    a.download = `${reportType}_${new Date().toISOString().slice(0, 10)}.pdf`;
    a.click();
    URL.revokeObjectURL(url);
  },
};

export default reportService;
