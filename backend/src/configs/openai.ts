// ============================================
// OpenAI Config
// ============================================

import OpenAI from 'openai';

let openaiClient: OpenAI | null = null;

export function getOpenAIClient(): OpenAI {
  if (!openaiClient) {
    const apiKey = process.env['OPENAI_API_KEY'];
    if (!apiKey) {
      throw new Error('OPENAI_API_KEY is not set in environment variables');
    }
    openaiClient = new OpenAI({ apiKey });
  }
  return openaiClient;
}

export const OPENAI_CONFIG = {
  model: process.env['OPENAI_MODEL'] ?? 'gpt-4o-mini',
  maxTokens: parseInt(process.env['OPENAI_MAX_TOKENS'] ?? '1000', 10),
  temperature: parseFloat(process.env['OPENAI_TEMPERATURE'] ?? '0.8'),
} as const;

// ---- Chat Completion Helper ----

export interface ChatMessage {
  role: 'system' | 'user' | 'assistant';
  content: string;
}

export async function chatCompletion(
  messages: ChatMessage[],
  options?: {
    model?: string;
    maxTokens?: number;
    temperature?: number;
    responseFormat?: 'text' | 'json';
  }
): Promise<string> {
  const client = getOpenAIClient();

  const response = await client.chat.completions.create({
    model: options?.model ?? OPENAI_CONFIG.model,
    messages,
    max_tokens: options?.maxTokens ?? OPENAI_CONFIG.maxTokens,
    temperature: options?.temperature ?? OPENAI_CONFIG.temperature,
    response_format:
      options?.responseFormat === 'json' ? { type: 'json_object' } : { type: 'text' },
  });

  const content = response.choices[0]?.message?.content;
  if (!content) {
    throw new Error('OpenAI returned empty response');
  }

  return content;
}

export async function chatCompletionJSON<T>(
  messages: ChatMessage[],
  options?: {
    model?: string;
    maxTokens?: number;
    temperature?: number;
  }
): Promise<T> {
  const raw = await chatCompletion(messages, { ...options, responseFormat: 'json' });
  return JSON.parse(raw) as T;
}
