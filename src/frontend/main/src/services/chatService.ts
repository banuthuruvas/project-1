import api from "./api";

export interface Conversation {
  id: number;
  title: string;
  userId: string;
  source: string;
  lastMessageAt: string;
  messageCount: number;
}

export interface ChatMessage {
  id: number;
  role: "user" | "assistant" | "system";
  content: string;
  createdAt: string;
  conversationId: number;
  tokenCount?: number;
}

export interface StreamEvent {
  content: string;
}

const chatService = {
  async getConversations(source?: string): Promise<Conversation[]> {
    const params = source ? `?source=${source}` : "";
    const res = await api.get(`/api/Chat/conversations${params}`);
    return res.data;
  },

  async getMessages(conversationId: number): Promise<ChatMessage[]> {
    const res = await api.get(
      `/api/Chat/conversations/${conversationId}/messages`,
    );
    return res.data;
  },

  async createConversation(
    source: string,
    title: string,
  ): Promise<Conversation> {
    const res = await api.post("/api/Chat/conversations", { title, source });
    return res.data;
  },

  async deleteConversation(id: number): Promise<void> {
    await api.delete(`/api/Chat/conversations/${id}`);
  },

  async renameConversation(id: number, title: string): Promise<void> {
    await api.post(`/api/Chat/conversations/${id}/rename`, { title });
  },

  async sendMessage(
    conversationId: number,
    content: string,
  ): Promise<ChatMessage> {
    const res = await api.post(
      `/api/Chat/conversations/${conversationId}/send`,
      { content },
    );
    return res.data;
  },

  async sendMessageStream(
    conversationId: number,
    content: string,
  ): Promise<Response> {
    const baseUrl = api.defaults.baseURL || "";
    const token = localStorage.getItem("auth_token");
    return fetch(`${baseUrl}/api/Chat/conversations/${conversationId}/send`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({ content }),
    });
  },

  async search(query: string): Promise<any> {
    const res = await api.get(
      `/api/Chat/search?q=${encodeURIComponent(query)}`,
    );
    return res.data;
  },
};

export default chatService;
