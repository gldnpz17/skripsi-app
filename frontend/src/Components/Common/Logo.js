const Logo = ({ size = 0 }) => (
  <div className='flex gap-2 items-center'>
    <img className={`${['h-6', 'h-8'][size]} aspect-square`} src='/skripsi-logo.png' />
    <span className={`font-bold ${['text-lg', 'text-3xl'][size]}`}>SCR<span className='text-primary-dark'>EⱯM</span></span>
  </div>
)

export { Logo }