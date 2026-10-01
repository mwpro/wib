import { Coins, Plus } from 'lucide-react'

interface StoreHeaderProps {
  walletBalance: number
  onAddReward: () => void
}

export function StoreHeader({ walletBalance, onAddReward }: StoreHeaderProps) {
  return (
    <div className="flex items-center justify-between gap-3 flex-wrap">
      <div>
        <h2 className="text-xl font-black tracking-tight text-stone-900 dark:text-white flex items-center gap-2.5">
          <span>Sklep z nagrodami</span>
        </h2>
        <span className="text-xs text-stone-500 dark:text-stone-400">
          Wymieniaj punkty z zadań na nagrody i realizuj kupony
        </span>
      </div>

      <div className="flex items-center gap-2">
        <span className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full bg-amber-100/90 dark:bg-amber-950/70 text-amber-950 dark:text-amber-300 border border-amber-300/80 dark:border-amber-800/80 text-xs font-bold shadow-xs">
          <Coins className="h-3.5 w-3.5 text-amber-700 dark:text-amber-400" />
          <span>Twój portfel: {walletBalance} pkt</span>
        </span>

        <button
          type="button"
          onClick={onAddReward}
          className="inline-flex items-center gap-1.5 px-3.5 py-2 bg-amber-400 hover:bg-amber-500 active:scale-95 text-amber-950 rounded-xl text-xs font-bold border border-amber-500/40 shadow-xs transition cursor-pointer"
        >
          <Plus className="h-4 w-4" />
          <span>Dodaj nagrodę</span>
        </button>
      </div>
    </div>
  )
}
