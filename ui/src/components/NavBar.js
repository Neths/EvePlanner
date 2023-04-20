import {Link, NavLink} from "react-router-dom";

export const NavBar = () => {

  let active = 'flex font-medium text-lg py-2 px-4 rounded-lg items-center justify-center gap-2 bg-white shadow hover:bg-white hover:text-gray-700'
  let pending = ''
  let normal = 'flex font-medium text-lg py-2 px-4 rounded-lg items-center justify-center gap-2 hover:shadow hover:bg-white'

  return (
    <nav id="header" className="overflow-hidden rounded-xl border border-gray-100 bg-gray-50 p-1">
      {/*<div className="overflow-hidden rounded-xl border border-gray-100 bg-gray-50 p-1">*/}
        <div className="flex flex-row items-center gap-2 text-sm font-medium">
            <NavLink
              to={'/'}
              className={({isActive, isPending}) => isPending ? pending : isActive ? active : normal }
            >
              Dashboard
            </NavLink>
            <NavLink
              to={'characters'}
              className={({isActive, isPending}) => isPending ? pending : isActive ? active : normal }
            >
              Characters
            </NavLink>
        </div>
      {/*</div>*/}
    </nav>
  )
}