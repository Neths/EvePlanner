import {useDispatch, useSelector} from "react-redux";
import {hideModal} from "../slices/modalSlice";


export const Modal = () => {
  const state = useSelector((state) => state.modal)
  const dispatch = useDispatch()

  if (!state.modal) {
    return null
  }

  return(
    <div className='bg-gray-700 absolute flex top-0 right-0 left-0 bottom-0 justify-center items-center z-10 bg-opacity-70'>
      <div className='py-10 font-medium'>
        <div className='shadow rounded p-1 mx-auto bg-white border-[1px] border-gray-200'>
          <div className='flex flex-row bg-gray-400 rounded-t bg-opacity-70 px-1'>
            <div className='grow'>{state.title}</div>
            <div onClick={ () => dispatch(hideModal()) } className="flex-none w-6">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="1.5"
                   stroke="currentColor"
                   className="w-4 h-4 cursor-pointer fill-current text-slate-500 hover:text-slate-900 m-1">
                <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12"/>
              </svg>
            </div>
          </div>

          <div className="text-lg text-gray-500 p-2">
            {state.content.map(item => (<p>{item}</p>))}
          </div>

          <div className="flex flex-row mt-6 space-x-2 justify-evenly">
            <a href="#" onClick={() => state.actionButtonOnClick() }
               className="w-full py-3 text-sm font-medium text-center text-white transition duration-150 ease-linear bg-red-600 border border-red-600 rounded-lg hover:bg-red-500">{state.actionButtonText}</a>
            <a href="#" onClick={ () => dispatch(hideModal()) }
               className="w-full py-3 text-sm text-center text-gray-500 transition duration-150 ease-linear bg-white border border-gray-200 rounded-lg hover:bg-gray-100">Cancel</a>
          </div>
        </div>
      </div>
    </div>
  )
}