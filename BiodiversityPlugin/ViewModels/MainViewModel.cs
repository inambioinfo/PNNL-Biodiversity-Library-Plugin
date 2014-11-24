﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BiodiversityPlugin.Models;
using GalaSoft.MvvmLight;

namespace BiodiversityPlugin.ViewModels
{

    public class MainViewModel: ViewModelBase
    {
        public ObservableCollection<OrgPhylum> Organisms { get; private set; }
        public ObservableCollection<PathwayGroup> Pathways { get; private set; } 

        public MainViewModel(IDataAccess data, string organismPath, string pathwaysPath)
        {
            Organisms = new ObservableCollection<OrgPhylum>(data.LoadOrganisms(organismPath));
            Pathways = new ObservableCollection<PathwayGroup>(data.LoadPathways(pathwaysPath));
        }
    }
}