using Post_knv_Server.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreLinq;

namespace Post_knv_Server.CalculationService
{
    /// <summary>
    /// class that contains the container calculation logic
    /// </summary>
    class ContainerCalculation
    {
        /// <summary>
        /// calculates a bet fit solution of the containers and the scan results
        /// </summary>
        /// <param name="pIntermediateResults">the scan results</param>
        /// <param name="pContainerCalibration">the calibrated container data</param>
        /// <returns>a best fit model of every scan result</returns>
        public static List<tModel> CalculateBestFitSolutions(List<IntermediateScanResultPackage> pIntermediateResults, List<ContainerConfigObject> pContainerCalibration)
        {
            //create result list
            List<tModel> resList = new List<tModel>();

            //start threads for every intermediate result package
            Parallel.ForEach(pIntermediateResults, result => 
            {
                //create combinations
                List<List<tContainerModel>> allCombinations = AllCombinationsOf<tContainerModel>(CalculateCombinationsForContainers(result, pContainerCalibration).ToArray());

                //rate each combination
                List<tModel> modelList = new List<tModel>();
                foreach (List<tContainerModel> t in allCombinations)
                    modelList.Add(new tModel(t,result.concavPlaneArea));

                //output best result (in %)
                resList.Add(modelList.MaxBy(t => t.pRating));
            });

            return resList;
        }

        /// <summary>
        /// calculates the maximum amount of possible containers for a given type
        /// </summary>
        /// <param name="pInputArea">the input area to calculate from</param>
        /// <param name="pContainer">the container object</param>
        /// <returns>the maximum amount of containers that fit into the area</returns>
        static int CalculateMaximumOfPossibleContainers(double pInputArea, ContainerConfigObject pContainer)
        {
            return (int)Math.Ceiling(pInputArea / (pContainer.containerDepth * pContainer.containerWidth)) + 1;
        }

        /// <summary>
        /// calculates all containers and the possible amounts and saves them into a list
        /// </summary>
        /// <param name="intermediateResult">the intermediate scanresult</param>
        /// <param name="pContainerCalibrations">the calibrated containers</param>
        /// <returns>a list which contains lists of the corresponding containers with amounts from 0 to max, where max is the maximum possible amount</returns>
        static List<List<tContainerModel>> CalculateCombinationsForContainers(IntermediateScanResultPackage intermediateResult, List<ContainerConfigObject> pContainerCalibrations)
        {
            //create list
            List<List<tContainerModel>> combinationList = new List<List<tContainerModel>>();

            //go through every container type and add up container amount combinations
            foreach(ContainerConfigObject containerType in pContainerCalibrations)
            {
                List<tContainerModel> modelList = new List<tContainerModel>();
                int maxContainers = CalculateMaximumOfPossibleContainers(intermediateResult.concavPlaneArea,containerType);
                for (int i = 0; i < maxContainers; i++)
                    modelList.Add(new tContainerModel(i, containerType));
                combinationList.Add(modelList);
            }
            return combinationList;
        }

        /// <summary>
        /// calculates all possible combinations of objects from multiple lists. uses 1 object per list
        /// </summary>
        /// <typeparam name="T">generic type</typeparam>
        /// <param name="sets">list with lists</param>
        /// <returns></returns>
        static List<List<T>> AllCombinationsOf<T>(params List<T>[] sets)
        {
            //source: http://stackoverflow.com/questions/545703/combination-of-listlistint
            // need array bounds checking etc for production
            var combinations = new List<List<T>>();

            // prime the data
            foreach (var value in sets[0])
                combinations.Add(new List<T> { value });

            foreach (var set in sets.Skip(1))
                combinations = AddExtraSet(combinations, set);

            return combinations;
        }

        /// <summary>
        /// helper method for AllCombinationsOf(param sets)
        /// </summary>
        static List<List<T>> AddExtraSet<T> (List<List<T>> combinations, List<T> set)
        {
            //source: http://stackoverflow.com/questions/545703/combination-of-listlistint
            var newCombinations = from value in set
                                  from combination in combinations
                                  select new List<T>(combination) { value };

            return newCombinations.ToList();
        }

        /// <summary>
        /// tModel class to rate the given combination
        /// </summary>
        public class tModel
        {
            /// <summary>
            /// the rating. the better the solution, the closer it gets to 100 (in percent)
            /// </summary>
            public double pRating { get; private set; }

            /// <summary>
            /// list of container models and amounts that have been used to determine the rating
            /// </summary>
            public List<tContainerModel> containerResults { get; private set; }

            /// <summary>
            /// constructor
            /// </summary>
            /// <param name="pContainerCombination">the combination of the containers</param>
            /// <param name="pInputArea">the input area to compare the containers to</param>
            public tModel(List<tContainerModel> pContainerCombination, double pInputArea)
            {
                this.containerResults = pContainerCombination;
                calculateRating(pInputArea);
            }

            /// <summary>
            /// calculates the rating for the given object
            /// </summary>
            /// <param name="pInputArea">input area to compare to</param>
            void calculateRating(double pInputArea)
            {
                //init var
                double fullArea = 0d;

                //sum up all containers base area used
                foreach (tContainerModel t in containerResults)
                    fullArea += ((t.containerType.containerDepth * t.containerType.containerWidth) *t.amountOfContainers);

                //calculate the rating
                this.pRating = (1 - (Math.Abs(pInputArea - fullArea) / pInputArea)) * 100;
            }
        }

        /// <summary>
        /// holding class for tModel calculation
        /// </summary>
        public class tContainerModel
        {
            /// <summary>
            /// aomunt of containers
            /// </summary>
            public int amountOfContainers { get; private set; }

            /// <summary>
            /// container type
            /// </summary>
            public ContainerConfigObject containerType { get; private set; }

            /// <summary>
            /// constructor
            /// </summary>
            /// <param name="pAmount">amount of containers</param>
            /// <param name="tContainerType">container type</param>
            public tContainerModel(int pAmount, ContainerConfigObject tContainerType)
            {
                this.amountOfContainers = pAmount;
                this.containerType = tContainerType;
            }
        }
    }
}
