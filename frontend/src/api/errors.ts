const messages: Record<string, string> = {
  'Wallet.NotEnoughPoints': 'Niewystarczająca liczba punktów w portfelu.',
  'Chore.NotFound': 'To zadanie już nie istnieje.',
  'RewardItem.NotFound': 'Ta nagroda już nie istnieje.',
  'RewardItem.Unavailable': 'Wybrana nagroda nie jest już dostępna.',
  'Voucher.NotFound': 'Ten kupon już nie istnieje.',
  'Voucher.AlreadyRedeemed': 'Kupon został już zrealizowany.',
}

export async function apiErrorMessage(response: Response, fallback: string): Promise<string> {
  try {
    const body: unknown = await response.json()
    const code = typeof body === 'object' && body !== null && 'code' in body ? body.code : undefined
    if (typeof code === 'string' && code in messages) {
      return messages[code]
    }
  } catch {
    return fallback
  }
  return fallback
}
