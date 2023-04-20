import {useDispatch, useSelector} from "react-redux";
import {hideModal, showModal} from "../slices/modalSlice";


export const Dashboard = () => {
  const dispatch = useDispatch()
  const modal = {
    title: 'mon titre',
    content: ['premiere ligne','seconde ligne'],
    actionButtonText: 'Delete',
    actionButtonOnClick: () => {
      alert('coucou')
      dispatch(hideModal())
    }
  }

  return (
    <>
      <h1>Dashboard</h1>
      <button onClick={() => dispatch(showModal(modal))} >Show Modal</button>
    </>
  )
}